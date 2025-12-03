using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
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
        private ObservableCollection<NavigationViewItem> _menuItems = new(); // Khởi tạo rỗng để tránh null
        [ObservableProperty]
        private ObservableCollection<NavigationViewItem> _footerMenuItems = new(); // Khởi tạo rỗng để tránh null
        [ObservableProperty] private string _selectedLanguage;
        private readonly INavigationService _navigationService;
        [ObservableProperty]
        private ObservableCollection<Device> _Devices;

        // ----- Toolbar -----
        [ObservableProperty] private ObservableCollection<string> lstLanguage;
        [ObservableProperty] private ObservableCollection<string> lstPort;
        [ObservableProperty] private string selectedPort;
        [ObservableProperty] private ObservableCollection<int> lstBaurate;
        [ObservableProperty] private int selectedBaudrate;
        [ObservableProperty] public ObservableCollection<int> lstResendData;
        [ObservableProperty] public int selectedResendData;
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


            _appSetting = appSetting;
            SelectedPort = _appSetting.Port;
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
            // Đăng ký event sau khi load xong
            _languageService.LanguageChanged += () => UpdateTexts();
            #region Default Setting
            SelectedLanguage = "中文";
            LstLanguage = new(["中文", "English", "Tiếng Việt", "ខ្មែរ"]);
            SelectedBaudrate = _appSetting.Baudrate;
            LstBaurate = new([1200, 9600, 19200, 38400, 115200]);
            LstPort = new(["COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM10"]);
            LstResendData = new([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
            #endregion
            // Inject các phụ thuộc
            _mySerialPort = mySerialPort;
            _context = powerTempWatchContext;
            SettingVM = settingViewModel;
            ToolVM = toolViewModel;
            // Khởi tạo Commands thủ công, sử dụng namespace đầy đủ để giải quyết lỗi mơ hồ
            NavigateCommand = new RelayCommand<object>(OnNavigate);
            SettingCommand = new RelayCommand<object>(ExecuteSettingForm);
            ChangeLanguageCommand = new RelayCommand<object>(ChangeLanguage);

            // Tạo collection rỗng ban đầu
            Devices = new ObservableCollection<Device>();
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
        public ICommand OpenCommand { get; }
        public ICommand ExitCommand { get; }

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
            
            MenuItems.Clear();
            FooterMenuItems.Clear();

            // 2a. Cài đặt Hệ thống (System Setting)
            var systemSettingMenu = new NavigationViewItem
            {
                Content = "Cài đặt Hệ thống",
                Icon = new SymbolIcon(SymbolRegular.Laptop24),
                // SỬA: Dùng Khối Khởi tạo Tập hợp để thêm items con.
                // Đây là cách đúng cho thuộc tính MenuItems của NavigationViewItem.
                MenuItems =
                {
                    new NavigationViewItem { Content = "Quản lý hoạt động", TargetPageType = typeof(Views.ActiveManagerView) },
                    new NavigationViewItem { Content = "Quản lý kiểu cảm biến", TargetPageType = typeof(Views.SensorTypeManagerView) },
                    new NavigationViewItem { Content = "Quản lý lệnh", TargetPageType = typeof(Views.CommandManagerView) },
                    new NavigationViewItem { Content = "Quản lý thiết bị", TargetPageType = typeof(Views.DeviceManagerView) }
                }
            };

            // 2b. Cài đặt Lưu trữ (Storage Setting)
            var storageSettingMenu = new NavigationViewItem
            {
                Content = "Cài đặt Lưu trữ",
                Icon = new SymbolIcon(SymbolRegular.DataArea24),
                TargetPageType = typeof(Views.SettingView)
            };

            // 2c. Cài đặt Bảo mật (Security Setting)
            var securitySettingMenu = new NavigationViewItem
            {
                Content = "Cài đặt Bảo mật",
                Icon = new SymbolIcon(SymbolRegular.LockClosed24),
                TargetPageType = typeof(Views.SettingView)
            };

            // 1. Tạo Menu Item Cấp 1: Cài đặt (Setting)
            var settingMenu = new NavigationViewItem
            {
                Content = SettingCommandText,
                Icon = new SymbolIcon(SymbolRegular.Settings24),
                MenuItems =
                {
                    systemSettingMenu,
                    storageSettingMenu,
                    securitySettingMenu
                }
            };

            // Khởi tạo các MenuItems chính (Sử dụng .Add() của ObservableCollection)
            MenuItems.Add(new NavigationViewItem
            {
                Content = DashboardCommandText,
                Icon = new SymbolIcon(SymbolRegular.ChartMultiple24),
                TargetPageType = typeof(Views.DashboardView)
            });
            MenuItems.Add(new NavigationViewItem
            {
                Content = ToolCommandText,
                Icon = new SymbolIcon(SymbolRegular.Toolbox24),
                TargetPageType = typeof(Views.ToolView)
            });
            // Dòng gây lỗi đã được sửa: Giờ đây MenuItems là ObservableCollection và hỗ trợ .Add()
            MenuItems.Add(settingMenu);

            // Khởi tạo FooterMenuItems
            FooterMenuItems.Add(new NavigationViewItem
            {
                Content = HelpCommandText,
                Icon = new SymbolIcon(SymbolRegular.QuestionCircle24),
                Command = new RelayCommand(OpenHelp)
            });
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
            RefreshPlayPauseText();
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
