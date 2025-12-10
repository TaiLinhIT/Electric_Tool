using System.Diagnostics;
using System.IO.Ports;
using System.Windows;

using Electric_Meter.Configs;
using Electric_Meter.Dto.SensorDataDto;
using Electric_Meter.Interfaces;
using Electric_Meter.Utilities;

namespace Electric_Meter.Services
{
    public class MySerialPortService
    {
        // Định nghĩa Delegate cho Event
        public event Action<Dictionary<string, double?>> DataUpdated;
        #region [ Fields (Private data) - Observable Properties ]
        //private readonly HttpClient _httpClient;
        private readonly IService _service;
        private readonly SemaphoreSlim _serialLock = new(1, 1);// SemaphoreSlim để đồng bộ hóa truy cập vào cổng COM
        private Dictionary<string, string> activeRequests = new Dictionary<string, string>(); // key = "address_requestName"
        private Dictionary<string, CancellationTokenSource> responseTimeouts = new Dictionary<string, CancellationTokenSource>();//
        private Dictionary<int, TaskCompletionSource<bool>> responseWaiters = new Dictionary<int, TaskCompletionSource<bool>>();
        private HashSet<string> processedRequests = new HashSet<string>();
        private Dictionary<int, Dictionary<string, double>> receivedDataByAddress = new Dictionary<int, Dictionary<string, double>>();
        private static readonly object lockObject = new object();
        public SerialPort _serialPort;
        public event SerialDataReceivedEventHandler Sdre;
        public string Port;
        public int Baudrate;
        bool _continue;
        Thread readThread;
        private AppSetting _appSetting;
        #endregion
        public MySerialPortService(IService service, AppSetting appSetting, SerialPort serialPort)
        {

            _serialPort = serialPort;
            _appSetting = appSetting;
            _service = service;

        }
        private Dictionary<int, int> _totalExpectedRequests = new Dictionary<int, int>();

        public async Task InitializeAsync() // Gọi hàm này khi khởi động Service
        {
            // Chỉ tải danh sách ControlCode 1 lần
            var allCodes = await _service.GetListControlcodeAsync();

            // Tính toán và cache tổng số request cho mỗi devid
            _totalExpectedRequests = allCodes
                .GroupBy(c => c.Devid)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public void Conn()
        {

            //_serialPort = new SerialPort();
            _serialPort.DataReceived += Sdre;
            _serialPort.ErrorReceived += _serialPort_ErrorReceived;
            _serialPort.PinChanged += _serialPort_PinChanged;
            _serialPort.PortName = _appSetting.Port;
            _serialPort.BaudRate = _appSetting.Baudrate;
            _serialPort.DataBits = 8;//数据长度：
            _serialPort.StopBits = StopBits.One;//停止位
            _serialPort.Handshake = Handshake.None;
            _serialPort.Parity = Parity.None;//校验方式
            _serialPort.ReadTimeout = 500; //设置超时读取时间
            _serialPort.WriteTimeout = 100;
            _serialPort.RtsEnable = true;
            //await SendDataToServer("5");
            try
            {
                _serialPort.Open();

            }
            catch (Exception e)
            {
                Tool.Log(string.Format("端口{0}打开失败:{1}", Port, e));
            }
        }


        private void _serialPort_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            Tool.Log(e.ToString());
        }

        private void _serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Tool.Log(e.ToString());
        }

        public void Stop()
        {
            _continue = false; _serialPort.Close(); _serialPort.Dispose();
        }

        public void Read()
        {
            try
            {
                _serialPort.Open();
                _continue = true;
            }
            catch (Exception e)
            {
                _continue = false;
                Debug.WriteLine(e);
            }

            while (_continue)
            {
                try
                {
                    string message = _serialPort.ReadLine();
                    Debug.WriteLine(message);
                }
                catch (Exception e)
                {
                    _continue = false;
                    Debug.WriteLine(e);
                }
                Thread.Sleep(100);
            }
            readThread.Join();
            _serialPort.Close();
        }
        ~MySerialPortService()
        {
            _serialPort.Close();
        }

        public void Write(string hexData)
        {
            try
            {
                // Gửi requestCode trước
                //byte[] requestNameBytes = Encoding.ASCII.GetBytes(requestCode);
                //_serialPort.Write(requestNameBytes, 0, requestNameBytes.Length);

                // Tách dữ liệu hex và gửi từng byte
                byte[] data = new byte[1];
                string strs = hexData.Replace(" ", "").Replace("\r", "").Replace("\n", "");

                if (strs.Length % 2 == 1)
                {
                    strs = strs.Insert(strs.Length - 1, "0");
                }

                foreach (char c in strs)
                {
                    if (!Uri.IsHexDigit(c))
                    {
                        throw new FormatException($"Ký tự không hợp lệ trong chuỗi hex: {c}");
                    }
                }

                for (int i = 0; i < strs.Length / 2; i++)
                {
                    data[0] = Convert.ToByte(strs.Substring(i * 2, 2), 16);
                    _serialPort.Write(data, 0, 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi: {ex.Message}");
                throw;
            }
        }


        public bool IsOpen()
        {
            return _serialPort.IsOpen;
        }

        #region [ Function communication ]
        public async void StartCommunication()
        {
            try
            {
                await InitializeAsync();
                // Bắt event nhận dữ liệu
                Sdre += SerialPort_DataReceived;
                // Mở cổng
                Conn();
                // Bắt đầu gửi request cho tất cả địa chỉ
                await SendRequestsToAllAddressesAsync();
            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi khi khởi động kết nối: {ex.Message}");
            }
        }

        #endregion
        #region [ Function Datareceived ]
        private List<byte> receiveBuffer = new List<byte>();
        private async void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var serialPort = (SerialPort)sender;
            try
            {
                int bytesToRead = serialPort.BytesToRead;

                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    serialPort.Read(buffer, 0, bytesToRead);
                    lock (receiveBuffer) // Khóa buffer khi thêm dữ liệu
                    {
                        receiveBuffer.AddRange(buffer);
                    }
                    // Gọi hàm xử lý buffer
                    _ = Task.Run(async () => await ProcessReceivedBuffer());
                    string hexString = BitConverter.ToString(buffer).Replace("-", " ");

                    // 1. Kiểm tra CRC
                    if (!Tool.CRC_PD(buffer))
                    {
                        Tool.Log($"CRC check failed for data: {hexString}");
                        return;
                    }

                    // 2. Lấy devid và Tìm matchedRequest một cách an toàn
                    int devid = buffer[0];
                    string requestKey = null;
                    string requestCode = null;

                    // --- START LOCKING VÀ LẤY REQUEST ---
                    lock (lockObject)
                    {
                        // Tìm Request gần nhất đang được gửi cho devid này.
                        // Trong mô hình mới, activeRequests chỉ chứa request VỪA MỚI GỬI
                        var matchedRequest = activeRequests.FirstOrDefault(kvp => kvp.Key.StartsWith($"{devid}_"));
                        requestKey = matchedRequest.Key;
                        requestCode = matchedRequest.Value;

                        // LOẠI BỎ: tcs và responseWaiters
                        // LOẠI BỎ: Logic processedRequests cũ, vì activeRequests chỉ chứa request đang chờ
                    }
                    // --- END LOCKING ---


                    if (!string.IsNullOrEmpty(requestKey))
                    {
                        Tool.Log($"Data received for {requestCode} at devid {devid}.");

                        // 3. Xử lý và lưu dữ liệu
                        // Chạy ParseAndStoreReceivedData để xử lý và lưu data (không cần lock)
                        await ParseAndStoreReceivedData(buffer, requestCode, devid);

                        // 4. Xóa request khỏi activeRequests ngay sau khi xử lý thành công
                        lock (lockObject)
                        {
                            // Chỉ xóa requestkey đang match
                            activeRequests.Remove(requestKey);
                        }
                        // LOẠI BỎ: tcs?.TrySetResult(true);

                    }
                    else
                    {
                        Tool.Log($"Received data from devid {devid} does not match any active request. Data: {hexString}");
                    }
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Error in DataReceived: {ex.Message}");
                });
            }
        }
        // Đã sửa lỗi: Loại bỏ `await` ra khỏi khối lock.
        private async Task ProcessReceivedBuffer()
        {
            var validFrames = new List<(byte[] Frame, int Devid, string RequestKey, string RequestCode)>();

            lock (lockObject)
            {
                while (receiveBuffer.Count >= 5) // Độ dài tối thiểu là 5 byte (Exception Response)
                {
                    int devid = receiveBuffer[0];
                    int functionCode = receiveBuffer[1];

                    // --- B1: XỬ LÝ EXCEPTION RESPONSE (FC + 0x80) ---
                    if ((functionCode & 0x80) != 0)
                    {
                        int exceptionLength = 5; // Slave ID (1) + FC (1) + Exception Code (1) + CRC (2)

                        if (receiveBuffer.Count >= exceptionLength)
                        {
                            byte[] errorFrame = receiveBuffer.GetRange(0, exceptionLength).ToArray();
                            receiveBuffer.RemoveRange(0, exceptionLength);

                            if (Tool.CRC_PD(errorFrame)) // Dù là lỗi vẫn nên kiểm tra CRC
                            {
                                int exceptionCode = errorFrame[2];
                                Tool.Log($"Modbus Exception for Slave {devid}. Code: 0x{exceptionCode:X2}.");
                            }
                            else
                            {
                                Tool.Log($"Modbus Exception Frame detected but CRC failed.");
                            }
                            continue; // Đã xử lý frame 5 byte này, tiếp tục kiểm tra buffer.
                        }
                        else
                        {
                            // Chưa đủ 5 byte cho Exception Frame, chờ thêm
                            break;
                        }
                    }

                    // --- B2: XỬ LÝ PHẢN HỒI DỮ LIỆU BÌNH THƯỜNG (FC = 0x03/0x04) ---
                    if (functionCode == 0x03 || functionCode == 0x04)
                    {
                        if (receiveBuffer.Count < 7)
                        {
                            break; // Chưa đủ 7 byte tối thiểu cho phản hồi dữ liệu
                        }

                        // Độ dài byte dữ liệu (Payload Length): byte thứ ba
                        int dataByteCount = receiveBuffer[2];
                        int expectedLength = 1 + 1 + 1 + dataByteCount + 2;

                        if (receiveBuffer.Count < expectedLength)
                        {
                            break; // Chưa đủ dữ liệu cho frame này, chờ thêm
                        }

                        // Đã đủ frame, cắt frame ra khỏi buffer
                        byte[] frame = receiveBuffer.GetRange(0, expectedLength).ToArray();
                        receiveBuffer.RemoveRange(0, expectedLength);

                        // 1. Kiểm tra CRC
                        if (!Tool.CRC_PD(frame))
                        {
                            Tool.Log($"CRC check failed for data: {BitConverter.ToString(frame).Replace("-", " ")}. Dropping invalid frame.");
                            continue;
                        }

                        // 2. Gom frame hợp lệ để xử lý bên ngoài lock (Logic tìm request vẫn giữ nguyên)
                        string requestKey = activeRequests.FirstOrDefault(kvp => kvp.Key.StartsWith($"{devid}_")).Key;
                        string requestCode = activeRequests.FirstOrDefault(kvp => kvp.Key.StartsWith($"{devid}_")).Value;

                        if (!string.IsNullOrEmpty(requestKey))
                        {
                            validFrames.Add((frame, devid, requestKey, requestCode));
                        }
                        else
                        {
                            Tool.Log($"Valid frame from {devid} but no active request.");
                        }
                    }
                    else
                    {
                        // FC không hợp lệ (Không phải Data/Exception) -> Lệch frame
                        Tool.Log($"Alignment Error: Invalid Function Code 0x{functionCode:X2}. Dropping 1 byte to realign.");
                        receiveBuffer.RemoveAt(0);
                    }
                }
            }
            // 2. XỬ LÝ DỮ LIỆU BẤT ĐỒNG BỘ (BÊN NGOÀI KHỐI LOCK)
            foreach (var (frame, devid, requestKey, requestCode) in validFrames)
            {
                Tool.Log($"Data received for {requestCode} at devid {devid}.");

                // 3. Xử lý và lưu dữ liệu (Dùng await ở đây là hợp lệ)
                await ParseAndStoreReceivedData(frame, requestCode, devid);

                // 4. Xóa request khỏi activeRequests và SetResult cho TaskCompletionSource (TCS)
                // Cần lock lại để đảm bảo an toàn khi thao tác với Dictionary
                lock (lockObject)
                {
                    // Chỉ xóa requestkey đang match
                    activeRequests.Remove(requestKey);

                    // Kích hoạt Task.WhenAny trong SendRequestAsync
                    if (responseWaiters.TryGetValue(devid, out var tcs))
                    {
                        tcs?.TrySetResult(true);
                        responseWaiters.Remove(devid);
                    }
                }
            }
        }
        #endregion
        #region [ Function Translate Data ]
        public async Task SendRequestsToAllAddressesAsync()
        {
            // [BỎ StartScanningLoop()] và KHÔI PHỤC LOGIC MULTI-TASK
            foreach (var item in await _service.GetListDeviceAsync())
            {
                int capturedAddress = item.devid;
                // Tạo Task cho mỗi Devid để chúng chạy độc lập
                _ = Task.Run(() => LoopRequestsForMachineAsync(capturedAddress));
            }
        }
        public async Task StartScanningLoop()
        {
            try
            {
                var allControlCodes = (await _service.GetListControlcodeAsync()).ToList();

                while (true)
                {
                    Stopwatch sw = Stopwatch.StartNew();

                    // Lặp qua tất cả request, xen kẽ giữa các Devid
                    foreach (var request in allControlCodes)
                    {
                        string requestCode = request.Code;
                        int devid = request.Devid;

                        await SendRequestAsync(requestCode, devid);
                    }

                    sw.Stop();
                    Tool.Log($"Hoàn tất chu kỳ quét toàn bộ ({allControlCodes.Count} requests) trong {sw.ElapsedMilliseconds} ms.");

                    // Đợi theo TimeSendRequest (Độ trễ giữa các chu kỳ quét toàn bộ)
                    await Task.Delay(TimeSpan.FromSeconds(_appSetting.TimeSendRequest));
                }
            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi trong vòng lặp quét tổng thể: {ex.Message}");
            }
        }
        private async Task LoopRequestsForMachineAsync(int devid)
        {
            var controlCodes = (await _service.GetListControlcodeAsync())
              .Where(c => c.Devid == devid).ToList();

            if (!controlCodes.Any()) return;

            while (true)
            {
                Stopwatch sw = Stopwatch.StartNew();
                // Vòng lặp quét các request của riêng Devid này
                foreach (var request in controlCodes)
                {
                    // CHỈ GỬI REQUEST, KHÔNG CHỜ PHẢN HỒI Ở ĐÂY
                    await SendRequestOnlyAsync(request.Code, devid);
                }
                sw.Stop();
                Tool.Log($"Hoàn tất chu kỳ quét cho devid {devid} trong {sw.ElapsedMilliseconds} ms.");
                await Task.Delay(TimeSpan.FromSeconds(_appSetting.TimeSendRequest));
            }
        }
        // Thay thế SendRequestAsync cũ bằng hàm mới này
        private async Task SendRequestOnlyAsync(string requestCode, int devid)
        {
            string requestKey = $"{devid}_{requestCode}";

            // KHÓA COM CHỈ ĐỂ GỬI (WRITE)
            await _serialLock.WaitAsync();

            try
            {
                // B1: Thêm vào activeRequests (Dùng LOCK)
                lock (lockObject)
                {
                    // Cần reset dữ liệu cũ
                    //activeRequests.Clear();
                    activeRequests[requestKey] = requestCode;
                }

                // B2 & B3: Xử lý CRC và Gửi
                byte[] requestBytes = _service.ConvertHexStringToByteArray(requestCode);
                string addressHex = _service.ConvertToHex(devid).PadLeft(2, '0');
                string requestString = addressHex + " " + BitConverter.ToString(requestBytes).Replace("-", " ");
                string CRCString = CRC.CalculateCRC(requestString);
                requestString += " " + CRCString;

                Tool.Log($"Đang gửi {requestKey}...");
                Write(requestString);
            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi khi gửi request {requestKey}: {ex.Message}");
            }
            finally
            {
                // GIẢI PHÓNG COM NGAY LẬP TỨC
                _serialLock.Release();

                // Chờ một khoảng thời gian nhỏ (ví dụ 100-200ms) để thiết bị phản hồi
                // và tránh việc gửi request quá nhanh làm nghẽn bộ đệm.
                await Task.Delay(500);
            }
        }
        private async Task SendRequestAsync(string requestCode, int devid)
        {
            TaskCompletionSource<bool> tcs = null;
            string requestKey = $"{devid}_{requestCode}";
            int timeoutMs = _appSetting.TimeOutReceive;

            await _serialLock.WaitAsync(); // KHÓA CẢ CHU TRÌNH GỬI + CHỜ

            try
            {
                lock (lockObject)
                {
                    // B1: Thêm vào activeRequests và khởi tạo TCS
                    activeRequests[requestKey] = requestCode;
                    tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); // Tùy chọn tốt hơn cho hiệu suất
                    responseWaiters[devid] = tcs;
                }

                // B2 & B3: Xử lý CRC và Gửi
                byte[] requestBytes = _service.ConvertHexStringToByteArray(requestCode);
                string addressHex = _service.ConvertToHex(devid).PadLeft(2, '0');
                string requestString = addressHex + " " + BitConverter.ToString(requestBytes).Replace("-", " ");
                string CRCString = CRC.CalculateCRC(requestString);
                requestString += " " + CRCString;

                Write(requestString);

                // B4: CHỜ PHẢN HỒI (HOẶC TIMEOUT)
                var delayTask = Task.Delay(TimeSpan.FromSeconds(timeoutMs));
                var completedTask = await Task.WhenAny(tcs.Task, delayTask);

                if (completedTask == delayTask)
                {
                    Tool.Log($"Timeout: Không nhận được phản hồi từ máy có địa chỉ {devid} cho request {requestCode}.");
                    // SetResult để đảm bảo TCS không bị treo nếu không có phản hồi
                    tcs.TrySetResult(false);
                }
            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi khi gửi/chờ request {requestKey}: {ex.Message}");
            }
            finally
            {
                // B5: ĐẢM BẢO XÓA activeRequests và responseWaiters BẰNG LOCK
                lock (lockObject)
                {
                    activeRequests.Remove(requestKey);
                    responseWaiters.Remove(devid);
                }
                _serialLock.Release(); // GIẢI PHÓNG COM
            }
        }
        private async Task StartResponseTimeoutAsync(string addressKey, CancellationToken cancellationToken)
        {
            try
            {
                int timeoutSeconds = _appSetting.TimeOutReceive; // đảm bảo bạn đã config nó trong appsettings.json

                await Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken);

                // Nếu không bị hủy, nghĩa là timeout xảy ra
                if (activeRequests.Keys.Any(k => k.StartsWith($"{addressKey}_")))
                {
                    //Tool.Log($"Timeout: Không nhận được phản hồi từ máy có địa chỉ {addressKey} sau {timeoutSeconds} giây.");
                    activeRequests = activeRequests
                        .Where(kvp => !kvp.Key.StartsWith($"{addressKey}_"))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
            }
            catch (TaskCanceledException)
            {
                // Bị huỷ đúng cách do có phản hồi đến
                Tool.Log($"Máy {addressKey} đã phản hồi đúng hạn.");
            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi khi xử lý timeout cho địa chỉ {addressKey}: {ex.Message}");
            }
        }
        private async Task ParseAndStoreReceivedData(byte[] data, string requestCode, int devid)
        {
            try
            {
                if (data.Length >= 9)
                {
                    int dataByteCount = data[2];
                    if (dataByteCount != 4 || data.Length < 5 + dataByteCount)
                    {
                        Tool.Log($"Invalid data for {requestCode} at devid {devid}: insufficient length.");
                        return;
                    }

                    // [1] KHỞI TẠO BIẾN TRƯỚC KHI SỬ DỤNG
                    double actualValue = 0.0; // Gán giá trị mặc định 0.0

                    bool foundMatch = false; // Biến cờ để kiểm tra đã tìm thấy request hợp lệ chưa
                    var controlCodes = await _service.GetListControlcodeAsync();
                    var matchedControlCode = controlCodes.FirstOrDefault(item => requestCode == item.Code);
                    if (matchedControlCode != null)
                    {
                        // B3: Xử lý dữ liệu dựa trên SensorType (Giữ nguyên logic xử lý "電力" và "溫度")
                        if (matchedControlCode.SensorType == "電力")
                        {
                            byte[] floatBytes = new byte[4];
                            Array.Copy(data, 3, floatBytes, 0, 4);
                            Array.Reverse(floatBytes);

                            float rawValue = BitConverter.ToSingle(floatBytes, 0);
                            actualValue = Math.Round((rawValue * matchedControlCode.Factor), 2);
                            foundMatch = true;
                        }
                        else if (matchedControlCode.SensorType == "溫度")
                        {
                            byte[] bytes = new byte[] { data[4], data[3], data[6], data[5] };

                            actualValue = Math.Round((BitConverter.ToSingle(bytes, 0)), 2);
                            foundMatch = true;
                        }
                    }

                    if (!foundMatch)
                    {
                        Tool.Log($"Unknown or unhandled SensorType for Code {requestCode} at devid {devid}. Data not processed.");
                        return;
                    }



                    // Sử dụng actualValue ở đây (đã được đảm bảo có giá trị)
                    lock (lockObject)
                    {
                        if (!receivedDataByAddress.ContainsKey(devid))
                            receivedDataByAddress[devid] = new Dictionary<string, double>();

                        // **KEY CHÍNH LÀ Code Modbus/OPC (requestCode)**
                        receivedDataByAddress[devid][requestCode] = actualValue;

                        // B5: Logic kiểm tra đủ request và kích hoạt lưu DB
                        if (_totalExpectedRequests.ContainsKey(devid) && receivedDataByAddress[devid].Count == _totalExpectedRequests[devid])// _totalExpectedRequests[devid])
                        {
                            var dataToSave = new Dictionary<string, double>(receivedDataByAddress[devid]);
                            receivedDataByAddress[devid].Clear();

                            _ = Task.Run(async () => await SaveAllData(devid, dataToSave));
                        }
                    }
                }
                else
                {
                    Tool.Log($"Incomplete data for {requestCode} at devid {devid}.");
                }
            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi khi phân tích dữ liệu {requestCode} tại địa chỉ {devid}: {ex.Message}");
                Tool.Log($"Dữ liệu gốc: {BitConverter.ToString(data)}");
            }
        }
        private async Task SaveAllData(int devid, Dictionary<string, double> dataForAddress)
        {
            try
            {

                var device = await _service.GetDeviceByDevidAsync(devid);

                if (device == null)
                {
                    Tool.Log($"Không tìm thấy Id device với địa chỉ {devid}");
                    return;
                }
                var now = DateTime.Now;

                // B2: Lấy danh sách ControlCode liên quan đến Devid này
                var controlCodes = await _service.GetControlcodeByDevidAsync(devid);

                // --------------------------------------------------------------------------
                // B3: Chuẩn bị giá trị cần lưu và Kích hoạt Event (MAPPING ĐỘNG)

                // Dictionary dùng để gửi update cho ViewModel. Key là Name/Description, Value là giá trị.
                var valuesForDisplay = new Dictionary<string, double?>();
                int savedCount = 0;


                foreach (var code in controlCodes)
                {
                    // code là ControlcodeDto
                    string dbCode = code.Code; // Sử dụng code.Code thay vì code.code

                    // Tìm giá trị trong Dictionary dữ liệu đã nhận
                    if (dataForAddress.TryGetValue(dbCode, out double actualValue))
                    {
                        // **Gửi cho ViewModel (Sử dụng NameControlcode)**
                        valuesForDisplay.Add(code.NameControlcode, actualValue); // <--- SỬA Ở ĐÂY

                        // **Kiểm tra và Lưu DB**

                        // 1. Kiểm tra giá trị âm cho Imp/Exp
                        // So sánh với NameControlcode
                        if ((code.NameControlcode == "Imp" && actualValue < 0) || (code.NameControlcode == "Exp" && actualValue < 0)) // <--- SỬA Ở ĐÂY
                        {
                            Tool.Log($"⚠ Giá trị {code.NameControlcode} không hợp lệ (âm) cho Code {dbCode}. Bỏ qua lưu trữ."); // <--- SỬA Ở ĐÂY
                            continue;
                        }

                        // 2. Tạo SensorData
                        var sensorData = new SensorDataDto
                        {
                            Devid = devid,
                            // Cần kiểm tra lại: Codeid trong DTO là CodeId. Nếu muốn lấy ID của Controlcode, 
                            // bạn nên dùng code.CodeId.
                            Codeid = code.CodeId, // <--- SỬA Ở ĐÂY
                            Value = actualValue,
                            Day = now
                        };

                        // 3. Lưu DB
                        bool isSaved = await _service.InsertToSensorDataAsync(sensorData);
                        if (isSaved)
                        {
                            savedCount++;
                        }
                    }
                    // else: Không tìm thấy giá trị cho Code này (có thể do lỗi truyền/nhận), bỏ qua.
                }
                // --------------------------------------------------------------------------

                // GỌI EVENT ĐỂ THÔNG BÁO CHO VIEWMODEL
                DataUpdated?.Invoke(valuesForDisplay);

                // Logging kết quả
                if (savedCount == 0)
                {
                    Tool.Log($"⚠ Không có bản ghi nào được lưu vào bảng SensorData cho địa chỉ {devid}.");
                }
                else
                {
                    Tool.Log($"→ Đã lưu {savedCount} bản ghi vào bảng SensorData cho địa chỉ {devid}.");
                }

            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi khi lưu dữ liệu cho địa chỉ {devid}: {ex.Message}");
            }
        }
        // Hàm tiện ích để lấy giá trị từ Dictionary dựa trên key có hậu tố `Address_X`
        private double? GetValueWithAddressSuffix(Dictionary<string, double> data, string key, int devid)
        {
            string fullKey = $"{key}_Address_{devid}";
            return data.ContainsKey(fullKey) ? data[fullKey] : null;
        }

        #endregion

        #region [ Function Device ]
        //private List<Device> GetDevices()
        //{
        //    return _context.devices.Where(d => d.ifshow == 1 && d.typeid == 7).ToList();
        //}
        #endregion
    }
}
