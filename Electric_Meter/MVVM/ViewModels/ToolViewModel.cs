using Electric_Meter.Configs;
using Electric_Meter.Core;
using Electric_Meter.Models;
using Electric_Meter.Services;
using Electric_Meter.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

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
        
        private Timer _timer;
        public int AddressCurrent { get;  set; }
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
        public ToolViewModel(Service service, AppSetting appSetting, MySerialPortService mySerialPortService,PowerTempWatchContext powerTempWatchContext) 
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
        private Dictionary<string, string> activeRequests = new Dictionary<string, string>();// đối tượng dùng làm khóa
        // Biến lưu trạng thái các request đã nhận
        private readonly Dictionary<string, double> receivedData = new Dictionary<string, double>();

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
                    if (activeRequests.TryGetValue(address.ToString(), out string requestName))
                    {
                        // Hủy trạng thái chờ phản hồi
                        if (responseTimeouts.ContainsKey(address.ToString()))
                        {
                            responseTimeouts[address.ToString()].Cancel();
                            responseTimeouts.Remove(address.ToString());
                        }

                        activeRequests.Remove(address.ToString());

                        // Xử lý dữ liệu nhận được
                        ParseAndStoreReceivedData(buffer, requestName, address);
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


        
        private Dictionary<int, Dictionary<string, double>> receivedDataByAddress = new Dictionary<int, Dictionary<string, double>>();

        private void ParseAndStoreReceivedData(byte[] data, string requestName, int address)
        {
            try
            {
                // Kiểm tra độ dài tối thiểu của dữ liệu
                if (data.Length >= 9)
                {
                    int dataByteCount = data[2];
                    if (dataByteCount != 4 || data.Length < 5 + dataByteCount)
                    {
                        Tool.Log($"Invalid data for {requestName} at address {address}: insufficient length.");
                        return;
                    }

                    // Lấy 4 byte dữ liệu (giả sử float)
                    byte[] floatBytes = new byte[4];
                    Array.Copy(data, 3, floatBytes, 0, 4);
                    Array.Reverse(floatBytes); // Đảo byte nếu cần

                    // Chuyển thành giá trị thực
                    float rawValue = BitConverter.ToSingle(floatBytes, 0);
                    double actualValue;

                    // Xử lý tùy thuộc vào requestName
                    switch (requestName)
                    {
                        case var name when name.StartsWith("U"): // Điện áp
                            actualValue = rawValue * 2; // Nhân với 2 nếu cần
                            break;

                        case var name when name.StartsWith("Exp") || name.StartsWith("Imp"): // Công suất hoặc năng lượng
                            actualValue = rawValue * 20.0f;
                            break;

                        case var name when name.StartsWith("I"): // Dòng điện
                            actualValue = rawValue / 1000.0f; // Chia cho 1000
                            break;

                        case var name when name.StartsWith("P"): // Công suất tức thời
                            actualValue = (rawValue * 2) / 1000.0f;
                            break;
                        default:
                            Tool.Log($"Unknown request type for {requestName} at address {address}.");
                            return;
                    }

                    // Làm tròn và lưu dữ liệu
                    actualValue = Math.Round(actualValue, 4);
                    lock (lockObject)
                    {
                        if (!receivedDataByAddress.ContainsKey(address))
                        {
                            receivedDataByAddress[address] = new Dictionary<string, double>();
                        }
                        receivedDataByAddress[address][requestName] = actualValue;
                    }

                    Tool.Log($"Address {address}, Request {requestName}, Value: {actualValue}");

                    // Kiểm tra nếu tất cả request của address đã hoàn thành
                    lock (lockObject)
                    {
                        if (receivedDataByAddress[address].Count == _appSetting.Requests.Count )
                        {
                            
                            StartSavingDataWithTimer(address); // Lưu dữ liệu
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
                Tool.Log($"Error parsing data for {requestName} at address {address}: {ex.Message}");
            }
        }




        private CancellationTokenSource _cancellationTokenSource;

        public async Task StartSavingDataWithTimer(int address)
        {
            // Thời gian lưu (giây -> mili giây)
            int saveInterval = _appSetting.TimeSaveToDataBase * 1000;

            // Đảm bảo hoạt động bất đồng bộ không nằm trong lock block
            lock (lockObject)
            {
                // Khởi động timer đồng bộ
                _timer = new Timer(async _ =>
                {
                    await SaveAllData(address); // Gọi phương thức lưu dữ liệu
                }, null, 0, saveInterval);
            }
        }



        public void StopSavingData()
        {
            _timer?.Dispose();
        }
        private async Task SaveAllData(int address)
        {
            try
            {
                Dictionary<string, double> dataForAddress;

                // Đồng bộ truy cập vào receivedDataByAddress
                lock (lockObject)
                {
                    if (!receivedDataByAddress.TryGetValue(address, out dataForAddress) || dataForAddress.Count < 12)
                    {
                        Console.WriteLine($"Dữ liệu cho địa chỉ {address} không hợp lệ hoặc không đủ các trường cần thiết.");
                        return;
                    }
                }

                // Lấy IdMachine từ cơ sở dữ liệu dựa trên address
                int idMachine = await _context.machines
                    .Where(m => m.Address == address)
                    .Select(m => m.Id)
                    .FirstOrDefaultAsync();

                if (idMachine == 0)
                {
                    Console.WriteLine($"Không tìm thấy IdMachine với địa chỉ {address}");
                    return;
                }

                // Lấy bản ghi gần nhất từ cơ sở dữ liệu
                var lastRecord = await _context.DvElectricDataTemps
                    .Where(d => d.IdMachine == idMachine)
                    .OrderByDescending(d => d.UploadDate)
                    .FirstOrDefaultAsync();

                // Tạo đối tượng DvElectricDataTemp mới
                var newRecord = new DvElectricDataTemp
                {
                    IdMachine = idMachine,
                    Ia = GetValueWithAddressSuffix(dataForAddress, "Ia", address),
                    Ib = GetValueWithAddressSuffix(dataForAddress, "Ib", address),
                    Ic = GetValueWithAddressSuffix(dataForAddress, "Ic", address),
                    Pt = GetValueWithAddressSuffix(dataForAddress, "Pt", address),
                    Pa = GetValueWithAddressSuffix(dataForAddress, "Pa", address),
                    Pb = GetValueWithAddressSuffix(dataForAddress, "Pb", address),
                    Pc = GetValueWithAddressSuffix(dataForAddress, "Pc", address),
                    Ua = GetValueWithAddressSuffix(dataForAddress, "Ua", address),
                    Ub = GetValueWithAddressSuffix(dataForAddress, "Ub", address),
                    Uc = GetValueWithAddressSuffix(dataForAddress, "Uc", address),
                    Exp = GetValueWithAddressSuffix(dataForAddress, "Exp", address),
                    Imp = GetValueWithAddressSuffix(dataForAddress, "Imp", address),
                    TotalElectric = (GetValueWithAddressSuffix(dataForAddress, "Exp", address) ?? 0) +
                                    (GetValueWithAddressSuffix(dataForAddress, "Imp", address) ?? 0),
                    UploadDate = DateTime.Now
                };

                // So sánh với bản ghi gần nhất
                if (lastRecord != null)
                {
                    bool isSimilar = Math.Abs((newRecord.Ia ?? 0) - (lastRecord.Ia ?? 0)) < 0.01 &&
                                     Math.Abs((newRecord.Ib ?? 0) - (lastRecord.Ib ?? 0)) < 0.01 &&
                                     Math.Abs((newRecord.Ic ?? 0) - (lastRecord.Ic ?? 0)) < 0.01 &&
                                     Math.Abs((newRecord.Exp ?? 0) - (lastRecord.Exp ?? 0)) < 0.01 &&
                                     Math.Abs((newRecord.Imp ?? 0) - (lastRecord.Imp ?? 0)) < 0.01 &&
                                     Math.Abs((newRecord.TotalElectric ?? 0) - (lastRecord.TotalElectric ?? 0)) < 0.01;

                    if (isSimilar)
                    {
                        Console.WriteLine($"Dữ liệu cho địa chỉ {address} không có thay đổi đáng kể, không lưu vào cơ sở dữ liệu.");
                        return;
                    }
                }

                // Lưu dữ liệu vào cơ sở dữ liệu
                await _service.InsertToElectricDataTempAsync(newRecord);

                Tool.Log($"Dữ liệu đã được lưu vào cơ sở dữ liệu cho địa chỉ {address}.");
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


        // Hàm tiện ích để lấy giá trị từ Dictionary
        private double? GetValue(Dictionary<string, double> data, string key)
        {
            return data.ContainsKey(key) ? data[key] : null;
        }



        public void Run(string area, List<int> addresses)
        {
            if (area != _appSetting.CurrentArea)
            {
                MessageBox.Show($"Area {area} does not match the current area.");
                return;
            }

            var tasks = new List<Task>();
            for (int i = 1; i <= 12; i++) // 3 Task song song
            {
                int taskId = i; // Cần lưu trữ taskId để tránh capture biến
                tasks.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        foreach (var request in _appSetting.Requests)
                        {
                            string requestName = request.Key;
                            string requestHex = request.Value;

                            // Gửi request
                            //SendRequest(requestName, requestHex, taskId);

                            // Chờ trước khi gửi request tiếp theo
                            Thread.Sleep(_appSetting.TimeSaveToDataBase * 100);
                        }
                    }
                }));
            }

            Task.WhenAll(tasks); // Đảm bảo tất cả Task được thực thi
        }


        private static readonly object lockObject = new object();
        
        private Dictionary<string, CancellationTokenSource> responseTimeouts = new Dictionary<string, CancellationTokenSource>();

        public async Task SendRequestsToAllAddressesAsync()
        {
            while (true)
            {
                
                for (int address = 1; address <= _appSetting.TotalMachine; address++)
                {
                    var tasks = new List<Task>();

                    // Duyệt qua từng request trong _appSetting.Requests
                    foreach (var request in _appSetting.Requests)
                    {
                        string requestName = $"{request.Key}_Address_{address}"; // Tạo tên request
                        string requestHex = request.Value; // Lấy giá trị hex của request

                        // Thêm task gửi request vào danh sách
                        tasks.Add(SendRequestAsync(requestName, requestHex, address));
                        await Task.Delay(500); // Delay giữa các request
                    }

                    // Đợi tất cả các request của address hiện tại hoàn thành
                    await Task.WhenAll(tasks);

                    // Delay giữa các địa chỉ
                    await Task.Delay(_appSetting.TimeSendRequest * 1000);
                }

                // Delay một khoảng thời gian trước khi lặp lại
                await Task.Delay(500); // Có thể điều chỉnh thời gian delay nếu cần
            }
        }

        private async Task SendRequestAsync(string requestName, string requestHex, int address)
        {
            try
            {
                // Làm sạch trạng thái request cũ (nếu tồn tại)
                lock (lockObject)
                {
                    if (activeRequests.ContainsKey(address.ToString()))
                    {
                        activeRequests.Remove(address.ToString());
                        if (responseTimeouts.ContainsKey(address.ToString()))
                        {
                            responseTimeouts[address.ToString()].Cancel();
                            responseTimeouts.Remove(address.ToString());
                        }
                    }
                }

                // Chuyển chuỗi hex thành byte array
                byte[] requestBytes = _service.ConvertHexStringToByteArray(requestHex);

                // Chuyển address từ hệ 10 sang hex
                string addressHex = _service.ConvertToHex(address);

                // Thêm "0" + address vào đầu chuỗi request
                string requestString = "0" + addressHex + " " + BitConverter.ToString(requestBytes).Replace("-", " ");

                // Tính CRC và thêm vào cuối chuỗi
                string CRCString = CRC.CalculateCRC(requestString);

                // Thêm CRC vào cuối chuỗi request
                requestString += " " + CRCString;

                // Cập nhật yêu cầu đang hoạt động với key là address
                lock (lockObject)
                {
                    activeRequests[address.ToString()] = requestName;  // Lưu trữ requestName theo address
                }

                // Tạo token cho timeout
                var cts = new CancellationTokenSource();
                lock (lockObject)
                {
                    responseTimeouts[requestName] = cts;
                }

                // Đặt trạng thái "chờ phản hồi"
                WaitForResponseAsync(requestName, cts.Token);

                // Gửi request qua SerialPort
                _mySerialPort.Write(requestString);
                Tool.Log($"Sent request {requestName} to address {address}: {requestString}");

                // Đợi phản hồi (nếu cần thời gian delay cho thiết bị xử lý)
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                // Log lỗi nếu xảy ra
                Tool.Log($"Error sending request {requestName}: {ex.Message}");
            }
        }



        private async void WaitForResponseAsync(string requestName, CancellationToken token)
        {
            try
            {
                // Đặt timeout (giả sử 5 giây)
                await Task.Delay(TimeSpan.FromSeconds(_appSetting.TimeSaveToDataBase), token);

                if (!token.IsCancellationRequested)
                {
                    // Hết thời gian chờ, chỉ xóa request nếu chưa nhận được phản hồi
                    lock (lockObject)
                    {
                        if (activeRequests.ContainsKey(requestName))
                        {
                            activeRequests.Remove(requestName);
                            responseTimeouts.Remove(requestName);
                        }
                    }

                    Tool.Log($"Timeout waiting for response for request {requestName}");
                }
            }
            catch (TaskCanceledException)
            {
                // Task bị hủy do đã nhận được phản hồi
                Tool.Log($"Request {requestName} successfully received response.");
            }
            catch (Exception ex)
            {
                Tool.Log($"Error in WaitForResponseAsync for request {requestName}: {ex.Message}");
            }
        }



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
