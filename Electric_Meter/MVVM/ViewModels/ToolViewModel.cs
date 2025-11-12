using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using Electric_Meter.Configs;
using Electric_Meter.Models;
using Electric_Meter.Services;
// Thêm partial vào lớp ToolViewModel

namespace Electric_Meter.MVVM.ViewModels
{
    // Cần thêm từ khóa partial vào đây
    public partial class ToolViewModel : ObservableObject
    {
        #region [ Fields (Private data) - Observable Properties ]
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
        public ToolViewModel(Service service, AppSetting appSetting, MySerialPortService mySerialPortService, PowerTempWatchContext powerTempWatchContext)
        {

            _context = powerTempWatchContext;
            _service = service;
            _appSetting = appSetting;
            _mySerialPort = mySerialPortService;
            // Đăng ký lắng nghe sự kiện ngay khi ViewModel được tạo
            _mySerialPort.DataUpdated += HandleDataUpdate;

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




        private async void ReloadData(string factory, int address)
        {
            if (!string.IsNullOrEmpty(Port))
            {

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
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Gọi hàm cập nhật dữ liệu của ViewModel
                UpdateToolViewData(data);
            });
        }
        #endregion

    }
}
