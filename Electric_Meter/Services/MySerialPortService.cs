using System.Diagnostics;
using System.IO.Ports;
using System.Windows;

using Electric_Meter.Configs;
using Electric_Meter.Dto.SensorDataDto;
using Electric_Meter.Interfaces;
using Electric_Meter.Models;
using Electric_Meter.Utilities;

using Microsoft.EntityFrameworkCore;

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
        private Dictionary<string, CancellationTokenSource> responseTimeouts = new Dictionary<string, CancellationTokenSource>();
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
        public MySerialPortService(IService service,  AppSetting appSetting, SerialPort serialPort)
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
                .GroupBy(c => c.CodeId)
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
        private async void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var serialPort = (SerialPort)sender;
                int bytesToRead = serialPort.BytesToRead;

                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    serialPort.Read(buffer, 0, bytesToRead);

                    string hexString = BitConverter.ToString(buffer).Replace("-", " ");

                    // Kiểm tra CRC
                    if (!Tool.CRC_PD(buffer))
                    {
                        Tool.Log($"CRC check failed for data: {hexString}");
                        return;
                    }

                    int devid = buffer[0];

                    // Lặp qua các activeRequests để tìm đúng request
                    var matchedRequest = activeRequests.FirstOrDefault(kvp => kvp.Key.StartsWith($"{devid}_"));

                    if (activeRequests.Count == 0)
                    {
                        //Tool.Log("ActiveRequests hiện đang trống.");
                    }


                    if (!string.IsNullOrEmpty(matchedRequest.Key))
                    {
                        string requestCode = matchedRequest.Value;
                        string requestKey = matchedRequest.Key;

                        // Tránh xử lý trùng trong cùng một lần nhận
                        if (processedRequests.Contains(requestKey))
                        {
                            Tool.Log($"Data for {requestCode} at devid {devid} already processed. Skipping...");
                            return;
                        }

                        // Đánh dấu là đã xử lý
                        processedRequests.Add(requestKey);

                        // Hủy timeout nếu có
                        if (responseTimeouts.ContainsKey(devid.ToString()))
                        {
                            responseTimeouts[devid.ToString()].Cancel();
                            responseTimeouts.Remove(devid.ToString());
                        }

                        activeRequests.Remove(requestKey);

                        // Gọi hàm xử lý
                        await ParseAndStoreReceivedData(buffer, requestCode, devid);

                        // XÓA KEY để lần sau vẫn xử lý được
                        processedRequests.Remove(requestKey);
                    }
                    else
                    {
                        Tool.Log($"Received data from devid {devid} does not match any active request.");
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


        #endregion
        #region [ Function Translate Data ]
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
            // Lấy ControlCode cho Devid/devid này
            var controlCodes = await _service.GetListControlcodeAsync();

            // Lọc chỉ những lệnh thuộc về devid hiện tại
            var machineControlCodes = controlCodes.Where(c => c.Devid == devid).ToList();

            while (true)
            {
                Stopwatch sw = Stopwatch.StartNew();

                // --- Vòng lặp gửi yêu cầu ---
                foreach (var request in machineControlCodes)
                {
                    long startTime = sw.ElapsedMilliseconds;

                    string requestCode = request.Code;

                    await SendRequestAsync(requestCode, devid);

                    long endTime = sw.ElapsedMilliseconds;
                    long elapsed = endTime - startTime;
                    int remainingDelay = 1000 - (int)elapsed;

                    if (remainingDelay > 0)
                    {
                        await Task.Delay(remainingDelay);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(_appSetting.TimeSendRequest));
            }
        }

        private async Task SendRequestAsync(string requestCode,  int devid)
        {
            try
            {
                await _serialLock.WaitAsync(); //Chỉ 1 máy được gửi tại 1 thời điểm

                // B1: Thêm vào activeRequests
                string requestKey = $"{devid}_{requestCode}";
                if (!activeRequests.ContainsKey(requestKey))
                {
                    activeRequests[requestKey] = requestCode;

                    //Thiết lập timeout nếu cần
                    var cts = new CancellationTokenSource();
                    responseTimeouts[devid.ToString()] = cts;
                    _ = StartResponseTimeoutAsync(devid.ToString(), cts.Token);
                }

                // B2: Xử lý dữ liệu hex
                byte[] requestBytes = _service.ConvertHexStringToByteArray(requestCode);
                string addressHex = _service.ConvertToHex(devid).PadLeft(2, '0');
                string requestString = addressHex + " " + BitConverter.ToString(requestBytes).Replace("-", " ");
                string CRCString = CRC.CalculateCRC(requestString);
                requestString += " " + CRCString;

                // B3: Gửi
                Write(requestString);

                await Task.Delay(300); // Chờ thiết bị phản hồi
            }
            finally
            {
                _serialLock.Release(); //Giải phóng cho máy khác gửi
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
                    Tool.Log($"Không tìm thấy IdMachine với địa chỉ {devid}");
                    return;
                }
                int Devid = device.devid;
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
                            Devid = Devid,
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
