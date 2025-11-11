using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // Cung cấp RelayCommand mới
using Electric_Meter.Configs;
using Electric_Meter.Models;
using Electric_Meter.MVVM.Views;
using Electric_Meter.Services;
using Newtonsoft.Json;
using Wpf.Ui;
using Wpf.Ui.Controls;

using Device = Electric_Meter.Models.Device;

namespace Electric_Meter.MVVM.ViewModels
{
    // Bắt buộc phải là partial class để Source Generator hoạt động
    public partial class MainViewModel : ObservableObject
    {
        #region [ Fields (Private data) - Observable Properties ]

        [ObservableProperty]
        private IEnumerable<NavigationViewItem> _menuItems;
        [ObservableProperty]
        private IEnumerable<NavigationViewItem> _footerMenuItems;
        [ObservableProperty] private string _selectedLanguage;
        private readonly INavigationService _navigationService;
        [ObservableProperty]
        private ObservableCollection<Device> _Devices;

        // ----- Toolbar -----
        [ObservableProperty] private ObservableCollection<string> _languages;
        [ObservableProperty] private ObservableCollection<string> _ports;
        [ObservableProperty] private string _selectedPort;
        [ObservableProperty] private ObservableCollection<int> _baudrates;
        [ObservableProperty] private int _selectedBaudrate;
        [ObservableProperty] private string _playPauseText;
        [ObservableProperty] private SymbolRegular _playPauseIcon = SymbolRegular.Play24;
        [ObservableProperty] private bool _isPlaying = false;
        [ObservableProperty] private string helpCommandText;
        [ObservableProperty] private string dashboardCommandText;
        [ObservableProperty] private string toolCommandText;
        [ObservableProperty] private string settingCommandText;
        [ObservableProperty] private string playCommandText;
        [ObservableProperty] private string pauseCommandText;




        // --- CÁC TRƯỜNG KHÁC (Không cần chuyển đổi) ---
        private readonly MySerialPortService _mySerialPort;
        private readonly AppSetting _appSetting;
        private readonly PowerTempWatchContext _context;
        private readonly LanguageService _languageService;
        private Dictionary<string, string> _currentLanguage;

        // Quản lý task cho từng địa chỉ máy
        private readonly Dictionary<int, CancellationTokenSource> _addressTasks = new();
        private readonly List<int> _selectedAddresses = new();

        #endregion


        #region [ Constructor ]
        public MainViewModel(
            LanguageService languageService,
            MySerialPortService mySerialPort,
            SettingViewModel settingViewModel,
            ToolViewModel toolViewModel,
            AppSetting appSetting,
            PowerTempWatchContext powerTempWatchContext,
            Service service)
        {
            _languageService = languageService;
            // Load ngôn ngữ mặc định trước
            string code = SelectedLanguage switch
            {
                "English" => "en",
                "中文" => "zh",
                "Tiếng Việt" => "vi",
                "ខ្មែរ" => "km",
                _ => "zh"
            };
            _languageService.LoadLanguage(code);
            // Đăng ký event sau khi load xong
            _languageService.LanguageChanged += () => UpdateTexts();
            #region Default Setting
            SelectedLanguage = "中文";
            Languages = new(["中文", "English", "Tiếng Việt", "ខ្មែរ"]);
            SelectedBaudrate = 9600;
            Baudrates = new([9600, 19200, 38400, 115200]);
            Ports = new(["COM1", "COM2", "COM3"]);
            #endregion
            SelectedPort = "COM3";
            // Select language


            // Khi đổi SelectedLanguage -> load JSON tương ứng
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedLanguage))
                {
                    string code = SelectedLanguage switch
                    {
                        "English" => "en",
                        "中文" => "zh",
                        "Tiếng Việt" => "vi",
                        "ខ្មែរ" => "km",
                        _ => "zh"
                    };
                    _languageService.LoadLanguage(code);
                }
            };
            // Inject các phụ thuộc
            _mySerialPort = mySerialPort;
            _context = powerTempWatchContext;
            SettingVM = settingViewModel;
            ToolVM = toolViewModel;
            _appSetting = appSetting;
            // Khởi tạo Commands thủ công, sử dụng namespace đầy đủ để giải quyết lỗi mơ hồ
            NavigateCommand = new RelayCommand<object>(OnNavigate);
            SettingCommand = new RelayCommand<object>(ExecuteSettingForm);
            ChangeLanguageCommand = new RelayCommand<object>(ChangeLanguage);

            SettingVM.OnDeviceLoadDefault += LoadDefaultDevice;

            // Tạo collection rỗng ban đầu
            Devices = new ObservableCollection<Device>();

            UpdateTexts();

            // Tải dữ liệu ban đầu
            LoadDefaultDevice();

            LoadLanguage("zh");

            TogglePlayPause();
        }
        #endregion


        #region [ Properties (Public for UI Binding) ]
        // ViewModels con
        public SettingViewModel SettingVM { get; }
        public ToolViewModel ToolVM { get; }


        

        public List<int> SelectedAddresses => _selectedAddresses;

        #endregion

        #region [ Commands ]
        // Các Command khởi tạo thủ công
        public ICommand NavigateCommand { get; }
        public ICommand ChangeLanguageCommand { get; }
        public ICommand SettingCommand { get; set; }

        
        [RelayCommand]
        private void TogglePlayPause()
        {
            IsPlaying = !IsPlaying;
            RefreshPlayPauseText();
            PlayPauseIcon = IsPlaying ? SymbolRegular.Pause24 : SymbolRegular.Play24;

            if (IsPlaying)
                _mySerialPort.StartCommunication(); // bật lại gửi nhận
            else
                _mySerialPort.Stop(); // dừng gửi nhận
        }

        [RelayCommand]
        private void OpenHelp()
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://your-help-link-or-docs",
                UseShellExecute = true
            });
        }

        #endregion
        #region [ Method - Menu ]
        private void InitializeMenuItems()
        {
            _menuItems = new[]
            {
                new NavigationViewItem()
                {
                    Content = DashboardCommandText,
                    Icon = new SymbolIcon(SymbolRegular.ChartMultiple24),
                    TargetPageType = typeof(Views.DashboardView)
                },
                new NavigationViewItem()
                {
                    Content = ToolCommandText,
                    Icon = new SymbolIcon(SymbolRegular.Toolbox24),
                    TargetPageType = typeof(Views.ToolView)
                }
            };
            _footerMenuItems = new[]
            {
                new NavigationViewItem()
                {
                    Content = SettingCommandText,
                    Icon = new SymbolIcon(SymbolRegular.Settings24),
                    TargetPageType = typeof(Views.SettingView)
                }

            };
        }
        #endregion
        #region [ Method - Refesh PlayPause Text ]
        private void RefreshPlayPauseText()
        {
            PlayPauseText = IsPlaying ? PauseCommandText : PlayCommandText;
        }

        #endregion

        #region [ Command Methods ]
        private void OnNavigate(object parameter)
        {
            if (parameter is SettingViewModel)
            {
                _navigationService?.Navigate(typeof(SettingView));
            }
            else if (parameter is ToolViewModel)
            {
                _navigationService?.Navigate(typeof(ToolView));
            }
            else if (parameter is DashboardViewModel)
            {
                _navigationService?.Navigate(typeof(DashboardView));
            }
        }

        private void ExecuteSettingForm(object parameter)
        {
            _navigationService?.Navigate(typeof(SettingView));
        }


        #endregion

        #region [ Methods - Language ]
        private void UpdateTexts()
        {
            HelpCommandText = _languageService.GetString("Help");
            DashboardCommandText = _languageService.GetString("Dashboard");
            ToolCommandText = _languageService.GetString("Tool");
            SettingCommandText = _languageService.GetString("Setting");
            PlayCommandText = _languageService.GetString("Play");
            PauseCommandText = _languageService.GetString("Pause");

            InitializeMenuItems();

        }
        #endregion


        public void LoadLanguage(string languageCode)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), $"Languages/{languageCode}.json");
            if (File.Exists(filePath))
            {
                var jsonData = File.ReadAllText(filePath);
                _currentLanguage = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
                OnPropertyChanged(""); // Thông báo tất cả thuộc tính đổi
            }
        }

        #region [ Device Management ]
        

        public void LoadDefaultDevice()
        {
            Devices.Clear();
            var DevicesFromDb = _context.devices.ToList();
            foreach (var Device in DevicesFromDb)
                Devices.Add(Device);
        }

        
        #endregion
        #region [ Method - Language ]
        private void ChangeLanguage(object languageCode)
        {
            if (languageCode is string code)
            {
                _languageService.LoadLanguage(code); // Thông báo cho tất cả ViewModel
                SelectedLanguage = code;
            }
        }

        #endregion

    }
}
