using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using Electric_Meter.Configs;
using Electric_Meter.Interfaces;
using Electric_Meter.Models;
using Electric_Meter.Services;
using Electric_Meter.Utilities;
// Thêm partial vào lớp ToolViewModel

namespace Electric_Meter.MVVM.ViewModels
{
    // Cần thêm từ khóa partial vào đây
    public partial class ToolViewModel : ObservableObject
    {
        #region [ Fields (Private data) - Observable Properties ]
        private readonly LanguageService _languageService;
        private ObservableCollection<ElectricDataDisplay> _electricDataTemp;
        public ObservableCollection<ElectricDataDisplay> ElectricDataTemp
        {
            get => _electricDataTemp;
            set => SetProperty(ref _electricDataTemp, value);
        }
        #endregion
        public string Port = string.Empty;
        public int Baudrate = 0;
        public string Factory = string.Empty;
        private DispatcherTimer _dispatcherTimer;
        public int IdDevice;
        private readonly IService _service;
        public MySerialPortService _mySerialPort;
        private readonly AppSetting _appSetting;
        private readonly PowerTempWatchContext _context;

        private readonly SemaphoreSlim _serialLock = new(1, 1);// SemaphoreSlim để đồng bộ hóa truy cập vào cổng COM


        private Timer _timer;
        private readonly Dictionary<int, Timer> _timers = new();

        public int AddressCurrent { get; set; }
        public string FactoryCode { get; private set; }
        #region Properties

        // Các thuộc tính [ObservableProperty] đã thêm
        [ObservableProperty] private string ia = "0.00";
        [ObservableProperty] private string ib = "0.00";
        [ObservableProperty] private string ic = "0.00";
        [ObservableProperty] private string ua = "0.00";
        [ObservableProperty] private string ub = "0.00";
        [ObservableProperty] private string uc = "0.00";
        [ObservableProperty] private string pt = "0.00";
        [ObservableProperty] private string pa = "0.00";
        [ObservableProperty] private string pb = "0.00";
        [ObservableProperty] private string pc = "0.00";
        [ObservableProperty] private string exp = "0.00";
        [ObservableProperty] private string imp = "0.00";
        [ObservableProperty] private string total = "0.00";
        [ObservableProperty] private bool isLoading = false;
        [ObservableProperty] private List<KeyValue> lstAssembling;
        [ObservableProperty] private KeyValue selectedAssembling;
        [ObservableProperty] private ObservableCollection<Device> lstDevice;
        [ObservableProperty] private Device selectedDevice;
        [ObservableProperty] private string searchQuery; // Đã thấy trong XAML, nhưng chưa
        #endregion


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
        public ToolViewModel(IService service, AppSetting appSetting, MySerialPortService mySerialPortService, PowerTempWatchContext powerTempWatchContext, LanguageService languageService)
        {
            _languageService = languageService;
            _context = powerTempWatchContext;
            _service = service;
            _appSetting = appSetting;
            _mySerialPort = mySerialPortService;
            _languageService.LanguageChanged += UpdateTexts;

            UpdateTexts();
            // Đăng ký lắng nghe sự kiện ngay khi ViewModel được tạo
            _mySerialPort.DataUpdated += HandleDataUpdate;

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
            ElectricDataTemp = new ObservableCollection<ElectricDataDisplay>()
            {
                new ElectricDataDisplay { Name = "Ua", Value = 0, Unit = "V" }, // Đã thêm Ua
                new ElectricDataDisplay { Name = "Ub", Value = 0, Unit = "V" },
                new ElectricDataDisplay { Name = "Uc", Value = 0, Unit = "V" },
                new ElectricDataDisplay { Name = "Ia", Value = 0, Unit = "A" },
                new ElectricDataDisplay { Name = "Ib", Value = 0, Unit = "A" },
                new ElectricDataDisplay { Name = "Ic", Value = 0, Unit = "A" },
                new ElectricDataDisplay { Name = "Pt", Value = 0, Unit = "kW" },
                new ElectricDataDisplay { Name = "Pa", Value = 0, Unit = "kW" },
                new ElectricDataDisplay { Name = "Pb", Value = 0, Unit = "kW" },
                new ElectricDataDisplay { Name = "Pc", Value = 0, Unit = "kW" },
                new ElectricDataDisplay { Name = "Exp", Value = 0, Unit = "kWh" },
                new ElectricDataDisplay { Name = "Imp", Value = 0, Unit = "kWh" }
                // Bạn có thể cần thêm Total
            };
            GetDefaultSetting();
        }

        public void StopTimer()
        {
            _dispatcherTimer.Stop();
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
        #region [ Method - Update Display ]


        public void UpdateToolViewData(Dictionary<string, double?> values)
        {
            // Cập nhật các ObservableProperty mới
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Helper để cập nhật thuộc tính string
                void UpdateStringProperty(string key, Action<string> setter)
                {
                    if (values.TryGetValue(key, out double? value) && value.HasValue)
                    {
                        // Định dạng giá trị double sang string với 2 chữ số thập phân
                        setter(value.Value.ToString("N2"));
                    }
                }

                UpdateStringProperty("Ia", v => Ia = v);
                UpdateStringProperty("Ib", v => Ib = v);
                UpdateStringProperty("Ic", v => Ic = v);
                UpdateStringProperty("Ua", v => Ua = v);
                UpdateStringProperty("Ub", v => Ub = v);
                UpdateStringProperty("Uc", v => Uc = v);
                UpdateStringProperty("Pt", v => Pt = v);
                UpdateStringProperty("Pa", v => Pa = v);
                UpdateStringProperty("Pb", v => Pb = v);
                UpdateStringProperty("Pc", v => Pc = v);
                UpdateStringProperty("Exp", v => Exp = v);
                UpdateStringProperty("Imp", v => Imp = v);

                // Nếu Total được tính là tổng của Exp và một giá trị khác (Total cũ trong code gốc),
                // bạn cần tìm cách tính toán lại ở đây (ví dụ: Exp + Imp, hoặc Exp + giá trị ban đầu của Total)
                // Hiện tại tôi sẽ tính Total = Exp + Imp (Ví dụ)
                if (values.TryGetValue("Exp", out double? expValue) && expValue.HasValue &&
                    values.TryGetValue("Imp", out double? impValue) && impValue.HasValue)
                {
                    // Đảm bảo không bị null khi tính toán
                    Total = (expValue.GetValueOrDefault() + impValue.GetValueOrDefault()).ToString("N2");
                }
                else if (values.TryGetValue("Total", out double? totalValue) && totalValue.HasValue)
                {
                    // Dùng giá trị Total nếu được truyền trực tiếp từ meter (dù không thấy trong SaveAllData)
                    Total = totalValue.Value.ToString("N2");
                }


                // Giữ lại logic cũ nếu bạn vẫn cần cập nhật ObservableCollection ElectricDataTemp
                foreach (var item in values)
                {
                    if (item.Value.HasValue)
                    {
                        var dataItem = ElectricDataTemp.FirstOrDefault(d => d.Name == item.Key);
                        if (dataItem != null)
                        {
                            dataItem.Value = item.Value.Value;
                        }
                    }
                }
            });
        }

        private void HandleDataUpdate(Dictionary<string, double?> data)
        {

            // Đảm bảo cập nhật UI trên luồng chính (Dispatcher)
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Gọi hàm cập nhật dữ liệu của ViewModel
                UpdateToolViewData(data);
            });
        }
        #endregion
        #region [ Methods - Language ]
        public void UpdateTexts()
        {
            // Điện áp
            PhaseAVoltageText = _languageService.GetString("PHASE A VOLTAGE");
            PhaseBVoltageText = _languageService.GetString("PHASE B VOLTAGE");
            PhaseCVoltageText = _languageService.GetString("PHASE C VOLTAGE");

            // Dòng điện
            PhaseACurrentText = _languageService.GetString("PHASE A CURRENT");
            PhaseBCurrentText = _languageService.GetString("PHASE B CURRENT");
            PhaseCCurrentText = _languageService.GetString("PHASE C CURRENT");

            // Công suất
            TotalPowerText = _languageService.GetString("TOTAL POWER");
            PhaseAPowerText = _languageService.GetString("Phase A Power");
            PhaseBPowerText = _languageService.GetString("Phase B Power");
            PhaseCPowerText = _languageService.GetString("Phase C Power");

            // Năng lượng
            ExportEnergyText = _languageService.GetString("EXPORT ENERGY");
            ImportEnergyText = _languageService.GetString("IMPORT ENERGY");
            TotalEnergyText = _languageService.GetString("TOTAL ENERGY");

            // Thành hình
            AssemblingText = _languageService.GetString("Assembling");
            // Khác
            SearchKeywordText = _languageService.GetString("Search keyword");
            SelectDevicePlaceholderText = _languageService.GetString("Select device...");
            Application.Current.Dispatcher.Invoke(() =>
            {
                SetupAssemblingList();
            });


        }
        #endregion

        #region [ Language Texts ]
        [ObservableProperty] private string phaseAVoltageText;
        [ObservableProperty] private string phaseBVoltageText;
        [ObservableProperty] private string phaseCVoltageText;
        [ObservableProperty] private string phaseACurrentText;
        [ObservableProperty] private string phaseBCurrentText;
        [ObservableProperty] private string phaseCCurrentText;
        [ObservableProperty] private string totalPowerText;
        [ObservableProperty] private string phaseAPowerText;
        [ObservableProperty] private string phaseBPowerText;
        [ObservableProperty] private string phaseCPowerText;
        [ObservableProperty] private string exportEnergyText;
        [ObservableProperty] private string importEnergyText;
        [ObservableProperty] private string totalEnergyText;
        [ObservableProperty] private string searchKeywordText;
        [ObservableProperty] private string selectDevicePlaceholderText;
        [ObservableProperty] private string assemblingText;
        #endregion
        #region [ Method ]
        private void GetDefaultSetting()
        {

            SetupAssemblingList();
            LstDevice = new ObservableCollection<Device>();
            SelectedAssembling = LstAssembling.FirstOrDefault();
            SearchQuery = string.Empty;
        }
        private void SetupAssemblingList()
        {
            // Bao bọc toàn bộ logic trong Dispatcher.Invoke() để đảm bảo an toàn luồng
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Cập nhật lại danh sách LstAssembling
                LstAssembling = new()
                {
                    new KeyValue { key = "A", value = $"{AssemblingText} A" },
                    new KeyValue { key = "B", value = $"{AssemblingText} B" },
                    new KeyValue { key = "C", value = $"{AssemblingText} C" },
                    new KeyValue { key = "D", value = $"{AssemblingText} D" }
                };

                // Đảm bảo giữ lại KeyValue đã chọn
                if (SelectedAssembling != null)
                {
                    var newSelected = LstAssembling.FirstOrDefault(x => x.key == SelectedAssembling.key);

                    if (newSelected != null)
                    {
                        // Gán trực tiếp vì đây là ObservableProperty (sẽ kích hoạt OnSelectedAssemblingChanged)
                        SelectedAssembling = newSelected;
                    }
                }
                // Nếu SelectedAssembling là null (lần chạy đầu), gán lại phần tử đầu tiên
                else
                {
                    SelectedAssembling = LstAssembling.FirstOrDefault();
                }
            });
        }
        partial void OnSelectedAssemblingChanged(KeyValue value)
        {
            if (value != null)
            {
                //// Lấy danh sách device theo assembling
                //LstDevice = new ObservableCollection<Device>(_service.GetDevicesByAssembling(value.key));

                //// Chọn device đầu tiên
                //SelectedDevice = LstDevice.FirstOrDefault();
            }
        }
        partial void OnSelectedDeviceChanged(Device value)
        {
            if (value != null)
            {
                // Lấy dữ liệu mới từ database theo device mới
                _ = LoadLatestDataAsync(value.devid);
            }
        }
        // Hàm async load dữ liệu mới
        private async Task LoadLatestDataAsync(int devid)
        {
            // Bắt đầu LOADING trên UI Thread
            Application.Current.Dispatcher.Invoke(() => { IsLoading = true; });

            Dictionary<string, double?> latestDict = null;

            try
            {
                // 1. Tải dữ liệu DB: Dùng ConfigureAwait(false) để cho phép phần tiếp theo của hàm 
                // chạy trên luồng nền (Thread Pool) và không cần quay lại UI Thread.
                var latestSensorData = await _service.GetLatestSensorByDeviceAsync(devid).ConfigureAwait(false);

                if (latestSensorData == null || latestSensorData.Count == 0)
                {
                    // Nếu không có dữ liệu, khởi tạo Dictionary với giá trị mặc định 0.0
                    latestDict = new Dictionary<string, double?>
                    {
                        { "Ia", 0.0 }, { "Ib", 0.0 }, { "Ic", 0.0 },
                        { "Ua", 0.0 }, { "Ub", 0.0 }, { "Uc", 0.0 },
                        { "Pt", 0.0 }, { "Pa", 0.0 }, { "Pb", 0.0 }, { "Pc", 0.0 },
                        { "Exp", 0.0 }, { "Imp", 0.0 },
                        { "Total", 0.0 }
                    };
                }
                else
                {
                    latestDict = latestSensorData
                       .Join(_context.controlcodes,
                             s => s.codeid,
                             c => c.codeid,
                             (s, c) => new { c.name, s.value })
                       // Sử dụng ToList() HOẶC ToDictionary() trên luồng nền là OK.
                       .ToDictionary(x => x.name, x => (double?)x.value);
                }

                // 3. Cập nhật UI: Bắt buộc phải quay lại UI Thread để tương tác với Properties/Control.
                if (latestDict != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Gọi hàm cập nhật UI chính
                        UpdateToolViewData(latestDict);
                    });
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi. Bạn có thể muốn cập nhật UI để hiển thị lỗi:
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Ví dụ: MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}");
                    Tool.Log($"Lỗi khi tải dữ liệu cho devid {devid}: {ex.Message}");
                });
            }
            finally
            {
                // 4. TẮT Loading: Bắt buộc phải quay lại UI Thread để thay đổi IsLoading.
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsLoading = false;
                });
            }
        }
        #endregion

    }
}
