using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Threading;

using Electric_Meter.Configs;
using Electric_Meter.Core;
using Electric_Meter.Models;
using Electric_Meter.Services;
using Electric_Meter.Utilities;

using Microsoft.EntityFrameworkCore;

namespace Electric_Meter.MVVM.ViewModels
{
    public class ToolViewModel : BaseViewModel
    {
        public string Port = string.Empty;
        public int Baudrate = 0;
        public string Factory = string.Empty;
        private DispatcherTimer _dispatcherTimer;
        public int IdMachine;
        private readonly Service _service;
        public MySerialPortService _mySerialPort;
        private readonly AppSetting _appSetting;
        private readonly PowerTempWatchContext _context;

        private readonly SemaphoreSlim _serialLock = new(1, 1);// SemaphoreSlim để đồng bộ hóa truy cập vào cổng COM


        private Timer _timer;
        private readonly Dictionary<int, Timer> _timers = new();

        public int AddressCurrent { get; set; }
        public string FactoryCode { get; private set; }
        #region Properties

        private double _ia, _ib, _ic, _ua, _ub, _uc, _pt, _pa, _pb, _pc, _exp, _imp, _total;

        public double Ia { get => _ia; set { _ia = value; OnPropertyChanged(nameof(Ia)); } }
        public double Ib { get => _ib; set { _ib = value; OnPropertyChanged(nameof(Ib)); } }
        public double Ic { get => _ic; set { _ic = value; OnPropertyChanged(nameof(Ic)); } }
        public double Ua { get => _ua; set { _ua = value; OnPropertyChanged(nameof(Ua)); } }
        public double Ub { get => _ub; set { _ub = value; OnPropertyChanged(nameof(Ub)); } }
        public double Uc { get => _uc; set { _uc = value; OnPropertyChanged(nameof(Uc)); } }
        public double Pt { get => _pt; set { _pt = value; OnPropertyChanged(nameof(Pt)); } }
        public double Pa { get => _pa; set { _pa = value; OnPropertyChanged(nameof(Pa)); } }
        public double Pb { get => _pb; set { _pb = value; OnPropertyChanged(nameof(Pb)); } }
        public double Pc { get => _pc; set { _pc = value; OnPropertyChanged(nameof(Pc)); } }
        public double Exp { get => _exp; set { _exp = value; OnPropertyChanged(nameof(Exp)); } }
        public double Imp { get => _imp; set { _imp = value; OnPropertyChanged(nameof(Imp)); } }
        public double Total { get => _total + _exp; set { _total = value; OnPropertyChanged(nameof(Total)); } }

        #endregion

        public ObservableCollection<DvElectricDataTemp> _electricDataTemp;
        public ObservableCollection<DvElectricDataTemp> ElectricDataTemp
        {
            get => _electricDataTemp;
            set
            {
                _electricDataTemp = value;
                OnPropertyChanged(nameof(ElectricDataTemp));
            }
        }
        private readonly DispatcherTimer _timerCurrent;
        private DateTime _currentTime;
        public DateTime CurrentTime
        {
            get { return _currentTime; }
            set
            {
                _currentTime = DateTime.Now;
                OnPropertyChanged(nameof(CurrentTime));
            }

        }

        //Constructor
        public ToolViewModel(Service service, AppSetting appSetting, MySerialPortService mySerialPortService, PowerTempWatchContext powerTempWatchContext)
        {

            _context = powerTempWatchContext;
            _service = service;
            _appSetting = appSetting;
            _mySerialPort = mySerialPortService;

            ElectricDataTemp = new ObservableCollection<DvElectricDataTemp> { new DvElectricDataTemp() };

            _dispatcherTimer = new DispatcherTimer();

            _dispatcherTimer.Interval = TimeSpan.FromSeconds(Convert.ToInt32(_appSetting.TimeReloadData));

            _dispatcherTimer.Tick += _dispatcherTimer_Tick;

            _currentTime = DateTime.Now;
            _timerCurrent = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timerCurrent.Tick += (s, e) =>
            {
                CurrentTime = DateTime.Now;
            };
            _timerCurrent.Start();
        }


        private void _dispatcherTimer_Tick(object? sender, EventArgs e)
        {
            ReloadData(FactoryCode, AddressCurrent);
        }

        public void StartTimer()
        {
            ReloadData(FactoryCode, AddressCurrent);
            _dispatcherTimer.Start();
        }
        public void StopTimer()
        {
            _dispatcherTimer.Stop();
        }


        public async void Start()
        {

            _mySerialPort.Port = Port;
            _mySerialPort.Baudrate = Baudrate;

            _mySerialPort.Sdre += SerialPort_DataReceived;
            _mySerialPort.Conn();
            await SendRequestsToAllAddressesAsync(); // Gọi phương thức gửi yêu cầu cho tất cả địa chỉ

        }
        #region Gửi request
        private async Task SendRequestAsync(string requestName, string requestHex, int address)
        {
            try
            {
                await _serialLock.WaitAsync(); //Chỉ 1 máy được gửi tại 1 thời điểm

                // B1: Thêm vào activeRequests
                string requestKey = $"{address}_{requestName}";
                if (!activeRequests.ContainsKey(requestKey))
                {
                    activeRequests[requestKey] = requestName;

                    //Thiết lập timeout nếu cần
                    var cts = new CancellationTokenSource();
                    responseTimeouts[address.ToString()] = cts;
                    _ = StartResponseTimeoutAsync(address.ToString(), cts.Token);
                }

                // B2: Xử lý dữ liệu hex
                byte[] requestBytes = _service.ConvertHexStringToByteArray(requestHex);
                string addressHex = _service.ConvertToHex(address).PadLeft(2, '0');
                string requestString = addressHex + " " + BitConverter.ToString(requestBytes).Replace("-", " ");
                string CRCString = CRC.CalculateCRC(requestString);
                requestString += " " + CRCString;

                // B3: Gửi
                _mySerialPort.Write(requestString);
                //Tool.Log($"Máy {address} gửi {requestName}: {requestString}");

                await Task.Delay(1000); // Chờ thiết bị phản hồi
            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi gửi request {requestName}: {ex.Message}");
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
                int timeoutSeconds = _appSetting.TimeSendRequest; // đảm bảo bạn đã config nó trong appsettings.json

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
        public async Task SendRequestsToAllAddressesAsync()
        {
            for (int address = 1; address <= _appSetting.TotalMachine; address++)
            {
                int capturedAddress = address; // tránh closure issue
                _ = Task.Run(() => LoopRequestsForMachineAsync(capturedAddress));
            }
        }
        private async Task LoopRequestsForMachineAsync(int address)
        {
            while (true)
            {
                //Tool.Log($"Máy {address}: Bắt đầu gửi dữ liệu");

                foreach (var request in _appSetting.Requests)
                {
                    string requestName = $"{request.Key}_Address_{address}";
                    await SendRequestAsync(requestName, request.Value, address);
                    await Task.Delay(10000);
                }

                Tool.Log($"Máy {address}: Hoàn tất vòng gửi dữ liệu. Chờ 5 phút...");
                await Task.Delay(TimeSpan.FromMinutes(_appSetting.TimeSendRequest)); // Hoặc dùng _appSetting.TimeReloadData
            }
        }
        #endregion
        //private Dictionary<string, string> activeRequests = new Dictionary<string, string>();// đối tượng dùng làm khóa
        private Dictionary<string, string> activeRequests = new Dictionary<string, string>(); // key = "address_requestName"

        // Biến lưu trạng thái các request đã nhận
        private readonly Dictionary<string, double> receivedData = new Dictionary<string, double>();

        private Dictionary<int, Dictionary<string, double>> receivedDataByAddress = new Dictionary<int, Dictionary<string, double>>();
        private HashSet<string> processedRequests = new HashSet<string>();
        #region Nhận dữ liệu
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

                    int address = buffer[0];

                    // Lặp qua các activeRequests để tìm đúng request
                    var matchedRequest = activeRequests.FirstOrDefault(kvp => kvp.Key.StartsWith($"{address}_"));

                    if (activeRequests.Count == 0)
                    {
                        //Tool.Log("ActiveRequests hiện đang trống.");
                    }
                    //else
                    //{
                    //    //Tool.Log("Danh sách activeRequests:");
                    //    foreach (var kvp in activeRequests)
                    //    {
                    //        Tool.Log($"Key = {kvp.Key}, Value = {kvp.Value}");
                    //    }
                    //}

                    //Tool.Log("Danh sách activeRequests hiện tại:");
                    //foreach (var kvp in activeRequests)
                    //{
                    //    Tool.Log($"  Key = {kvp.Key}, Value = {kvp.Value}");
                    //}

                    //Tool.Log($"Matched request: Key = {matchedRequest.Key}, Value = {matchedRequest.Value}");

                    if (!string.IsNullOrEmpty(matchedRequest.Key))
                    {
                        string requestName = matchedRequest.Value;
                        string requestKey = matchedRequest.Key;

                        // Tránh xử lý trùng trong cùng một lần nhận
                        if (processedRequests.Contains(requestKey))
                        {
                            Tool.Log($"Data for {requestName} at address {address} already processed. Skipping...");
                            return;
                        }

                        // Đánh dấu là đã xử lý
                        processedRequests.Add(requestKey);

                        // Hủy timeout nếu có
                        if (responseTimeouts.ContainsKey(address.ToString()))
                        {
                            responseTimeouts[address.ToString()].Cancel();
                            responseTimeouts.Remove(address.ToString());
                        }

                        activeRequests.Remove(requestKey);

                        // Gọi hàm xử lý
                        ParseAndStoreReceivedData(buffer, requestName, address);

                        // XÓA KEY để lần sau vẫn xử lý được
                        processedRequests.Remove(requestKey);
                    }
                    else
                    {
                        Tool.Log($"Received data from address {address} does not match any active request.");
                    }
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error in DataReceived: {ex.Message}");
                });
            }
        }


        #endregion
        #region Dịch dữ liệu
        private void ParseAndStoreReceivedData(byte[] data, string requestName, int address)
        {
            try
            {
                if (data.Length >= 9)
                {
                    int dataByteCount = data[2];
                    if (dataByteCount != 4 || data.Length < 5 + dataByteCount)
                    {
                        Tool.Log($"Invalid data for {requestName} at address {address}: insufficient length.");
                        return;
                    }

                    // Giải mã giá trị float
                    byte[] floatBytes = new byte[4];
                    Array.Copy(data, 3, floatBytes, 0, 4);
                    Array.Reverse(floatBytes); // Đảo byte nếu cần

                    float rawValue = BitConverter.ToSingle(floatBytes, 0);
                    double actualValue;

                    // Phân loại theo tên
                    if (requestName.StartsWith("U") || requestName.StartsWith("Exp") || requestName.StartsWith("Imp") || requestName.StartsWith("P"))
                        actualValue = rawValue * 20.0; //actualValue = rawValue / 10.0;
                    else if (requestName.StartsWith("I"))
                        actualValue = rawValue / 1000.0;
                    else
                    {
                        Tool.Log($"Unknown request type for {requestName} at address {address}.");
                        return;
                    }

                    actualValue = Math.Round(actualValue, 2);

                    lock (lockObject)
                    {
                        if (!receivedDataByAddress.ContainsKey(address))
                            receivedDataByAddress[address] = new Dictionary<string, double>();

                        receivedDataByAddress[address][requestName] = actualValue;

                        //Tool.Log($"Nhận {requestName} = {actualValue} tại địa chỉ {address}. Hiện có {receivedDataByAddress[address].Count}/{_appSetting.Requests.Count}");

                        // Kiểm tra đủ số lượng request
                        if (receivedDataByAddress[address].Count == _appSetting.Requests.Count)
                        {
                            //Tool.Log($"Đã đủ {_appSetting.Requests.Count} trường dữ liệu tại địa chỉ {address}, tiến hành lưu vào DB...");

                            // Gọi hàm lưu trong background
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await SaveAllData(address);

                                    lock (lockObject)
                                    {
                                        receivedDataByAddress[address].Clear();
                                        processedRequests.RemoveWhere(k => k.StartsWith($"{address}_"));
                                    }

                                    Tool.Log($"Lưu thành công dữ liệu cho địa chỉ {address}");
                                }
                                catch (Exception ex)
                                {
                                    Tool.Log($"Lỗi khi lưu dữ liệu cho địa chỉ {address}: {ex.Message}");
                                }
                            });
                        }
                    }
                }
                else
                {
                    Tool.Log($"Incomplete data for {requestName} at address {address}.");
                }
            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi khi phân tích dữ liệu {requestName} tại địa chỉ {address}: {ex.Message}");
                Tool.Log($"Dữ liệu gốc: {BitConverter.ToString(data)}");
            }
        }

        #endregion





        private CancellationTokenSource _cancellationTokenSource;

        public async Task StartSavingDataWithTimer(int address)
        {
            int saveInterval = _appSetting.TimeSaveToDataBase * 1000;

            lock (lockObject)
            {
                if (_timers.ContainsKey(address))
                {
                    Tool.Log($"Timer cho địa chỉ {address} đã tồn tại, không tạo lại.");
                    return;
                }

                //Tool.Log($"Khởi tạo timer lưu dữ liệu cho địa chỉ {address} mỗi {saveInterval / 1000} giây.");

                var timer = new Timer(async _ =>
                {
                    try
                    {
                        Tool.Log($"Bắt đầu lưu dữ liệu cho địa chỉ {address}...");
                        await SaveAllData(address);
                    }
                    catch (Exception ex)
                    {
                        Tool.Log($"Lỗi trong timer của địa chỉ {address}: {ex.Message}");
                    }
                }, null, 0, saveInterval);

                _timers[address] = timer;
            }
        }





        public void StopSavingData()
        {
            foreach (var timer in _timers.Values)
            {
                timer?.Dispose();
            }
            _timers.Clear();
        }
        private async Task SaveAllData(int address)
        {
            try
            {
                //Tool.Log($"Đang chuẩn bị lấy dữ liệu đã nhận cho địa chỉ {address}...");

                Dictionary<string, double> dataForAddress;

                lock (lockObject)
                {
                    if (!receivedDataByAddress.TryGetValue(address, out dataForAddress))
                    {
                        Tool.Log($"Không tìm thấy dữ liệu cho địa chỉ {address}.");
                        return;
                    }

                    if (dataForAddress.Count < 12)
                    {
                        //Tool.Log($"Dữ liệu không đủ trường cần thiết cho địa chỉ {address}. Đã nhận {dataForAddress.Count} trường.");
                        return;
                    }
                }

                //Tool.Log($"Đang tìm IdMachine tương ứng với địa chỉ {address}...");

                var device = _appSetting.devices.FirstOrDefault(m => m.address == address);
                if (device == null)
                {
                    Tool.Log($"Không tìm thấy IdMachine với địa chỉ {address}");
                    return;
                }

                int idMachine = device.devid;
                //Tool.Log($"Tìm thấy IdMachine = {idMachine} cho địa chỉ {address}");

                var now = DateTime.Now;

                // 1. Chuẩn bị giá trị cần lưu
                var valuesToSave = new Dictionary<string, double?>
                {
                    { "Ub", GetValueWithAddressSuffix(dataForAddress, "Ub", address) },
                    { "Uc", GetValueWithAddressSuffix(dataForAddress, "Uc", address) },
                    { "Ia", GetValueWithAddressSuffix(dataForAddress, "Ia", address) },
                    { "Ib", GetValueWithAddressSuffix(dataForAddress, "Ib", address) },
                    { "Ic", GetValueWithAddressSuffix(dataForAddress, "Ic", address) },
                    { "Pt", GetValueWithAddressSuffix(dataForAddress, "Pt", address) },
                    { "Pa", GetValueWithAddressSuffix(dataForAddress, "Pa", address) },
                    { "Pb", GetValueWithAddressSuffix(dataForAddress, "Pb", address) },
                    { "Pc", GetValueWithAddressSuffix(dataForAddress, "Pc", address) },
                    { "Exp", GetValueWithAddressSuffix(dataForAddress, "Exp", address) },
                    { "Imp", GetValueWithAddressSuffix(dataForAddress, "Imp", address) }
                };

                // 2. Lấy danh sách control code theo devid
                var controlCodes = await _context.controlcodes
                    .Where(c => c.devid == idMachine)
                    .ToListAsync();

                int savedCount = 0;

                foreach (var item in valuesToSave)
                {
                    if (!item.Value.HasValue) continue;

                    var code = controlCodes.FirstOrDefault(c => c.name == item.Key);
                    if ((code.name == "Imp" && item.Value.Value < 0) || (code.name == "Exp" && item.Value.Value < 0))
                    {
                        Tool.Log($"⚠ Giá trị {item.Key} không hợp lệ (âm) cho địa chỉ {address}. Bỏ qua lưu trữ.");
                        continue; // Bỏ qua nếu giá trị âm
                    }

                    if (code != null)
                    {

                        var sensorData = new SensorData
                        {
                            devid = idMachine,
                            codeid = code.codeid,
                            value = item.Value.Value,
                            day = now
                        };
                        //Tool.Log($"→ Đang lưu: devid={sensorData.devid}, codeid={sensorData.codeid}, value={sensorData.value}, day={sensorData.day}");

                        // Gọi và kiểm tra kết quả lưu
                        bool isSaved = await _service.InsertToSensorDataAsync(sensorData);
                        if (isSaved)
                        {
                            savedCount++;
                        }
                    }
                }

                // Logging kết quả
                if (savedCount == 0)
                {
                    Tool.Log($"⚠ Không có bản ghi nào được lưu vào bảng SensorData cho địa chỉ {address}.");
                }
                else
                {
                    Tool.Log($"→ Đã lưu {savedCount} bản ghi vào bảng SensorData cho địa chỉ {address}.");
                }

            }
            catch (Exception ex)
            {
                Tool.Log($"Lỗi khi lưu dữ liệu cho địa chỉ {address}: {ex.Message}");
            }
        }



        // Hàm tiện ích để lấy giá trị từ Dictionary dựa trên key có hậu tố `Address_X`
        private double? GetValueWithAddressSuffix(Dictionary<string, double> data, string key, int address)
        {
            string fullKey = $"{key}_Address_{address}";
            return data.ContainsKey(fullKey) ? data[fullKey] : null;
        }



        private static readonly object lockObject = new object();

        private Dictionary<string, CancellationTokenSource> responseTimeouts = new Dictionary<string, CancellationTokenSource>();










        private async void ReloadData(string factory, int address)
        {
            if (!string.IsNullOrEmpty(Port))
            {
                List<DvElectricDataTemp> data = await _service.GetListDataAsync(address);

                if (data != null)
                {
                    var find = data;

                    // Clear the existing items and add new ones
                    ElectricDataTemp.Clear();
                    foreach (var item in find)
                    {
                        ElectricDataTemp.Add(item);
                    }
                }
            }
        }
        public void Close()
        {
            try
            {
                _mySerialPort.Stop();

            }
            catch (Exception ex)
            {

            }
        }
    }
}
