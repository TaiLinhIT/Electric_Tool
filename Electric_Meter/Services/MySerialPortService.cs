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
        private readonly IService _service;
        private readonly SemaphoreSlim _serialLock = new(1, 1); // SemaphoreSlim để đồng bộ hóa truy cập vào cổng COM
        private Dictionary<string, string> activeRequests = new Dictionary<string, string>(); // key = "address_requestName"
        private Dictionary<int, TaskCompletionSource<bool>> responseWaiters = new Dictionary<int, TaskCompletionSource<bool>>();
        private Dictionary<int, Dictionary<string, double>> receivedDataByAddress = new Dictionary<int, Dictionary<string, double>>();

        // SỬ DỤNG DUY NHẤT lockObject để bảo vệ các tài nguyên chia sẻ
        private static readonly object lockObject = new object();

        public SerialPort _serialPort;
        public event SerialDataReceivedEventHandler Sdre;
        public string Port;
        public int Baudrate;
        bool _continue;
        Thread readThread;
        private AppSetting _appSetting;
        private List<byte> receiveBuffer = new List<byte>(); // Đưa receiveBuffer lên đầu
        #endregion

        public MySerialPortService(IService service, AppSetting appSetting, SerialPort serialPort)
        {
            _serialPort = serialPort;
            _appSetting = appSetting;
            _service = service;
        }

        private Dictionary<int, int> _totalExpectedRequests = new Dictionary<int, int>();

        public async Task InitializeAsync()
        {
            var allCodes = await _service.GetListControlcodeAsync();
            _totalExpectedRequests = allCodes
                .GroupBy(c => c.Devid)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public void Conn()
        {
            _serialPort.DataReceived += Sdre;
            _serialPort.ErrorReceived += _serialPort_ErrorReceived;
            _serialPort.PinChanged += _serialPort_PinChanged;
            _serialPort.PortName = _appSetting.Port;
            _serialPort.BaudRate = _appSetting.Baudrate;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.Parity = Parity.None;

            // TĂNG READTIMEOUT VÀ BUFFERS SIZE
            _serialPort.ReadTimeout = 1000; // Tăng lên 1 giây
            _serialPort.WriteTimeout = 100;
            _serialPort.ReadBufferSize = 4096; // Tăng kích thước buffer

            _serialPort.RtsEnable = true;
            try
            {
                _serialPort.Open();
            }
            catch (Exception e)
            {
                // Sử dụng _appSetting.Port thay vì Port
                Tool.Log(string.Format("Port {0} open failed: {1}", _appSetting.Port, e));
            }
        }

        private void _serialPort_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            Tool.Log($"PinChanged: {e.EventType}");
        }

        private void _serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Tool.Log($"ErrorReceived: {e.EventType}");
        }

        public void Stop()
        {
            _continue = false;
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
            }
        }

        // Đã loại bỏ hàm Read() dùng ReadLine() vì nó không phù hợp với giao tiếp Modbus RTU (Binary)
        // và hàm Write(string hexData) cũ lỗi thời.

        // ------------- SỬA LỖI GỬI TỪNG BYTE TẠI ĐÂY -------------

        private byte[] ConvertHexStringToByteArray(string hexString)
        {
            string cleanHex = hexString.Replace(" ", "").Replace("\r", "").Replace("\n", "");

            if (cleanHex.Length % 2 != 0)
            {
                Tool.Log("Lỗi: Chuỗi Hex phải có số ký tự chẵn.");
                return Array.Empty<byte>();
            }

            byte[] bytes = new byte[cleanHex.Length / 2];
            for (int i = 0; i < cleanHex.Length; i += 2)
            {
                try
                {
                    bytes[i / 2] = Convert.ToByte(cleanHex.Substring(i, 2), 16);
                }
                catch (FormatException ex)
                {
                    Tool.Log($"Lỗi chuyển đổi byte {cleanHex.Substring(i, 2)}: {ex.Message}");
                    throw;
                }
            }
            return bytes;
        }

        // HÀM GHI MỚI: GHI TOÀN BỘ MẢNG BYTE TRONG MỘT LẦN GỌI
        public void Write(byte[] data)
        {
            if (!_serialPort.IsOpen)
            {
                Tool.Log("Lỗi: Cổng COM chưa mở.");
                return;
            }
            try
            {
                _serialPort.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi khi ghi cổng COM: {ex.Message}");
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
                Sdre += SerialPort_DataReceived;
                Conn();
                await SendRequestsToAllAddressesAsync();
            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi khi khởi động kết nối: {ex.Message}");
            }
        }

        #endregion

        #region [ Function Datareceived ]

        private async void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var serialPort = (SerialPort)sender;
            try
            {
                int bytesToRead = serialPort.BytesToRead;

                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    // ĐỌC DỮ LIỆU TỪ PORT TRƯỚC KHI LOCK
                    serialPort.Read(buffer, 0, bytesToRead);
                    //Tool.Log($"Bytes to read: {bytesToRead}");

                    // SỬ DỤNG lockObject THAY VÌ lock(receiveBuffer)
                    lock (lockObject)
                    {
                        receiveBuffer.AddRange(buffer);
                    }

                    string hexString = BitConverter.ToString(buffer).Replace("-", " ");
                    //Tool.Log($"Nhận Thô <- [{serialPort.PortName}] {hexString}");

                    // GỌI HÀM XỬ LÝ KHÔNG DÙNG await ĐỂ KHÔNG CHẶN EVENT
                    _ = Task.Run(async () => await ProcessReceivedBuffer());
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

        private async Task ProcessReceivedBuffer()
        {
            var validFrames = new List<(byte[] Frame, int Devid, string RequestKey, string RequestCode)>();

            // SỬ DỤNG lockObject ĐỂ ĐỒNG BỘ VỚI DataReceived
            lock (lockObject)
            {
                while (receiveBuffer.Count >= 5)
                {
                    int devid = receiveBuffer[0];
                    int functionCode = receiveBuffer[1];

                    // 1. KIỂM TRA ĐỊA CHỈ (SLAVE ID)
                    bool isKnownSlave = _totalExpectedRequests.ContainsKey(devid);
                    if (!isKnownSlave)
                    {
                        Tool.Log($"Alignment Error: Unknown Slave ID 0x{devid:X2}. Dropping 1 byte to realign.");
                        receiveBuffer.RemoveAt(0); // Loại bỏ byte rác
                        continue;
                    }

                    // 2. TÌM KEY REQUEST HIỆN TẠI (Đảm bảo có request đang chờ)
                    string requestKey = activeRequests.Keys.FirstOrDefault(k => k.StartsWith($"{devid}_"));

                    if (string.IsNullOrEmpty(requestKey))
                    {
                        // Nếu không có request đang hoạt động, có thể đây là phản hồi cũ/rác.
                        Tool.Log($"Alignment Warning: Frame from known Slave {devid} but no active request found. Dropping 1 byte.");
                        receiveBuffer.RemoveAt(0);
                        continue;
                    }

                    // Lấy requestCode tương ứng
                    string requestCode = activeRequests[requestKey];

                    // --- B1: XỬ LÝ EXCEPTION RESPONSE (FC + 0x80) ---
                    if ((functionCode & 0x80) != 0)
                    {
                        int exceptionLength = 5;

                        if (receiveBuffer.Count >= exceptionLength)
                        {
                            byte[] errorFrame = receiveBuffer.GetRange(0, exceptionLength).ToArray();
                            receiveBuffer.RemoveRange(0, exceptionLength);

                            if (Tool.CRC_PD(errorFrame))
                            {
                                int exceptionCode = errorFrame[2];
                                Tool.Log($"Modbus Exception for Slave {devid}. Code: 0x{exceptionCode:X2}. Request: {requestCode}");

                                // Gom frame lỗi để xử lý logic bên ngoài lock (Set result/cancel TCS)
                                validFrames.Add((errorFrame, devid, requestKey, requestCode));
                            }
                            else
                            {
                                // LỖI CRC cho Exception Frame -> Nhiễu/timing
                                Tool.Log($"Modbus Exception Frame detected but CRC failed for {requestCode}. Dropping invalid frame.");
                            }
                            continue;
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

                        int dataByteCount = receiveBuffer[2];
                        int expectedLength = 1 + 1 + 1 + dataByteCount + 2; // ID+FC+ByteCount+Data+CRC

                        if (receiveBuffer.Count < expectedLength)
                        {
                            break; // Chưa đủ dữ liệu cho frame này, chờ thêm
                        }

                        // Đã đủ frame, cắt frame ra khỏi buffer
                        byte[] frame = receiveBuffer.GetRange(0, expectedLength).ToArray();
                        receiveBuffer.RemoveRange(0, expectedLength);

                        // 3. Kiểm tra CRC
                        if (!Tool.CRC_PD(frame))
                        {
                            Tool.Log($"CRC check failed for data: {BitConverter.ToString(frame).Replace("-", " ")}. Dropping invalid frame.");
                            continue;
                        }

                        // Gom frame hợp lệ để xử lý bên ngoài lock
                        validFrames.Add((frame, devid, requestKey, requestCode));
                    }
                    else
                    {
                        // FC không hợp lệ (Không phải Data/Exception) -> Lệch frame
                        Tool.Log($"Alignment Error: Invalid Function Code 0x{functionCode:X2}. Dropping 1 byte to realign.");
                        receiveBuffer.RemoveAt(0);
                        continue;
                    }
                }
            }

            // 2. XỬ LÝ DỮ LIỆU BẤT ĐỒNG BỘ (BÊN NGOÀI KHỐI LOCK)
            foreach (var (frame, devid, requestKey, requestCode) in validFrames)
            {
                //Tool.Log($"Data received for {requestCode} at devid {devid}.");

                // 3. Xử lý và lưu dữ liệu 
                if ((frame[1] & 0x80) == 0) // Chỉ Parse Data nếu không phải Exception Frame
                {
                    await ParseAndStoreReceivedData(frame, requestCode, devid);
                }

                // 4. Xóa request khỏi activeRequests và SetResult cho TaskCompletionSource (TCS)
                // Cần lock lại để đảm bảo an toàn khi thao tác với Dictionary
                lock (lockObject)
                {
                    activeRequests.Remove(requestKey);

                    // Kích hoạt Task.WhenAny trong SendRequestAsync
                    if (responseWaiters.TryGetValue(devid, out var tcs))
                    {
                        // Dùng TrySetResult vì nó an toàn hơn
                        tcs?.TrySetResult(true);
                        responseWaiters.Remove(devid);
                    }
                }
            }
        }
        #endregion

        #region [ Function Translate Data ]
        // Giữ nguyên logic LoopRequestsForMachineAsync và SendRequestsToAllAddressesAsync
        public async Task SendRequestsToAllAddressesAsync()
        {
            foreach (var item in await _service.GetListDeviceAsync())
            {
                int capturedAddress = item.devid;
                _ = Task.Run(() => LoopRequestsForMachineAsync(capturedAddress));
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
                foreach (var request in controlCodes)
                {
                    await SendRequestOnlyAsync(request.Code, devid);
                }
                sw.Stop();
                Tool.Log($"Hoàn tất chu kỳ quét cho devid {devid} trong {sw.ElapsedMilliseconds} ms.");

                // Độ trễ giữa các CHU KỲ (Nếu bạn cần chạy nhanh hơn, hãy giảm TimeSendRequest)
                await Task.Delay(TimeSpan.FromSeconds(_appSetting.TimeSendRequest));
            }
        }

        private async Task SendRequestOnlyAsync(string requestCode, int devid)
        {
            string requestKey = $"{devid}_{requestCode}";

            await _serialLock.WaitAsync();

            try
            {
                lock (lockObject)
                {
                    activeRequests[requestKey] = requestCode;
                }

                byte[] pduBytes = _service.ConvertHexStringToByteArray(requestCode.Replace(" ", ""));

                // B1: Chuẩn bị ADU = SlaveID + PDU
                // Slave ID
                byte slaveId = (byte)devid;

                // Gộp SlaveID và PDU
                List<byte> adu = new List<byte> { slaveId };
                adu.AddRange(pduBytes);

                // B2: Tính CRC cho ADU (CRC.CalculateCRC phải nhận chuỗi Hex của ADU)
                string aduHexNoSpace = BitConverter.ToString(adu.ToArray()).Replace("-", "");
                string CRCString = CRC.CalculateCRC(aduHexNoSpace); // Cần đảm bảo hàm CRC.CalculateCRC hoạt động đúng với chuỗi Hex không khoảng trắng

                // B3: Chuyển thành MẢNG BYTE CUỐI CÙNG
                string finalRequestString = aduHexNoSpace + CRCString;
                byte[] finalRequestBytes = ConvertHexStringToByteArray(finalRequestString); // Sử dụng hàm ConvertHexStringToByteArray của class này

                //Tool.Log($"Đang gửi {requestKey}: {BitConverter.ToString(finalRequestBytes).Replace("-", " ")}");
                Write(finalRequestBytes); // Gửi toàn bộ mảng byte
            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi khi gửi request {requestKey}: {ex.Message}");
            }
            finally
            {
                await Task.Delay(300);
                _serialLock.Release();

            }
        }

        // Loại bỏ hàm SendRequestAsync và StartScanningLoop không dùng đến để đơn giản hóa logic

        private async Task ParseAndStoreReceivedData(byte[] data, string requestCode, int devid)
        {
            // GIỮ NGUYÊN LOGIC CỦA BẠN VÌ NÓ CÓ VẺ ĐÚNG CHO VIỆC PHÂN TÍCH DATA
            try
            {
                if (data.Length >= 9) // Kiểm tra chiều dài tối thiểu cho dữ liệu 4 byte (9 byte)
                {
                    int dataByteCount = data[2];

                    // Thêm kiểm tra nghiêm ngặt hơn
                    if (dataByteCount != 4 || data.Length != 5 + dataByteCount)
                    {
                        Tool.Log($"Invalid data length for {requestCode} at devid {devid}. Expected: {5 + dataByteCount}, Got: {data.Length}.");
                        return;
                    }

                    double actualValue = 0.0;
                    bool foundMatch = false;
                    var controlCodes = await _service.GetListControlcodeAsync();
                    var matchedControlCode = controlCodes.FirstOrDefault(item => requestCode == item.Code);

                    if (matchedControlCode != null)
                    {
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
                            // Lưu ý: Chuỗi byte này có vẻ đang đảo hai cặp word, hãy kiểm tra lại cấu trúc byte từ tài liệu thiết bị
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

                    // LƯU DATA VÀ KIỂM TRA ĐỦ REQUEST (SỬ DỤNG lockObject)
                    lock (lockObject)
                    {
                        if (!receivedDataByAddress.ContainsKey(devid))
                            receivedDataByAddress[devid] = new Dictionary<string, double>();

                        receivedDataByAddress[devid][requestCode] = actualValue;

                        if (_totalExpectedRequests.ContainsKey(devid) && receivedDataByAddress[devid].Count == _totalExpectedRequests[devid])
                        {
                            var dataToSave = new Dictionary<string, double>(receivedDataByAddress[devid]);
                            receivedDataByAddress[devid].Clear();

                            _ = Task.Run(async () => await SaveAllData(devid, dataToSave));
                        }
                    }
                }
                else
                {
                    Tool.Log($"Incomplete data/Short frame detected for {requestCode} at devid {devid}.");
                }
            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi khi phân tích dữ liệu {requestCode} tại địa chỉ {devid}: {ex.Message}");
                Tool.Log($"Dữ liệu gốc: {BitConverter.ToString(data)}");
            }
        }

        // Giữ nguyên hàm SaveAllData và GetValueWithAddressSuffix
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

        private double? GetValueWithAddressSuffix(Dictionary<string, double> data, string key, int devid)
        {
            string fullKey = $"{key}_Address_{devid}";
            return data.ContainsKey(fullKey) ? data[fullKey] : null;
        }

        #endregion
    }
}
