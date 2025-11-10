
using System.Collections.Generic; // Thêm
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading; // Thêm
using System.Windows;
using System.Windows.Controls;
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
using Wpf.Ui.Controls.Interfaces;

using Machine = Electric_Meter.Models.Machine;

namespace Electric_Meter.MVVM.ViewModels
{
    // Bắt buộc phải là partial class để Source Generator hoạt động
    public partial class MainViewModel : ObservableObject
    {
        #region [ Fields (Private data) - Observable Properties ]

       

        [ObservableProperty]
        private string _currentFactory;

        [ObservableProperty]
        private ObservableCollection<Machine> _machines;

        [ObservableProperty]
        private ICollection<object> _MenuItems; // Đổi tên từ NavigationItems

        [ObservableProperty]
        private ICollection<object> _FooterMenuItems; // Đổi tên từ FooterNavigationItems

        
        private readonly INavigationService _navigationService;

        // ✅ THÊM: IPageService (Cần thiết cho Wpf.Ui)
        private readonly IPageService _pageService;



        // --- CÁC TRƯỜNG KHÁC (Không cần chuyển đổi) ---
        private readonly AppSetting _appSetting;
        private readonly PowerTempWatchContext _context;
        private readonly LanguageService _languageService;
        private Dictionary<string, string> _currentLanguage;

        // Quản lý task cho từng địa chỉ máy
        private readonly Dictionary<int, CancellationTokenSource> _addressTasks = new();
        private readonly List<int> _selectedAddresses = new();

        #endregion

        #region [ Fields (Private data) - Custom Properties ]
        private string _selectedLanguage;
        private string _assemblingText;
        #endregion

        #region [ Constructor ]
        public MainViewModel(
            SettingViewModel settingViewModel,
            ToolViewModel toolViewModel,
            AppSetting appSetting,
            PowerTempWatchContext powerTempWatchContext)
        {
            // Inject các phụ thuộc
            _context = powerTempWatchContext;
            SettingVM = settingViewModel;
            ToolVM = toolViewModel;
            _appSetting = appSetting;
            // Khởi tạo Commands thủ công, sử dụng namespace đầy đủ để giải quyết lỗi mơ hồ
            NavigateCommand = new RelayCommand<object>(OnNavigate);
            SettingCommand = new RelayCommand<object>(ExecuteSettingForm);
            ChangeLanguageCommand = new RelayCommand<object>(ChangeLanguage);

            //CurrentViewModel = SettingVM;

            // Lắng nghe event từ SettingVM
            SettingVM.NewButtonCreated += OnNewButtonCreated;
            SettingVM.OnMachineLoadDefault += LoadDefaultMachine;

            // Tạo collection rỗng ban đầu
            Machines = new ObservableCollection<Machine>();
            CurrentFactory = _appSetting.CurrentArea;

            // Khởi tạo service ngôn ngữ
            _languageService = new LanguageService();
            UpdateTexts();
            LoadLanguage("en");

            // Tải dữ liệu ban đầu
            LoadDefaultMachine();
            InitializeNavigationItems();

            // Khởi động ToolVM
            ToolVM.Start();
            MenuItems = new ObservableCollection<object>
            {
                new NavigationViewItem("Home", SymbolRegular.Home24, typeof(SettingView)),
                new NavigationViewItem("Data", SymbolRegular.DataHistogram24, typeof(ToolView))
            };

                FooterMenuItems = new ObservableCollection<object>
            {
                new NavigationViewItem("Settings", SymbolRegular.Settings24, typeof(SettingView))
            };
        }
        #endregion
        private void InitializeNavigationItems()
        {
            // Sử dụng các View/Page Type của bạn (Giả định SettingView, ToolView là các Page)
            NavMenuItems = new ObservableCollection<object>
            {
                new NavigationViewItem()
                {
                    Content = "Dashboard",
                    Icon = new SymbolIcon(SymbolRegular.Home24),
                    TargetPageType = typeof(SettingView) // Dashboard (Giả định là SettingView của bạn)
                },
                new NavigationViewItem()
                {
                    Content = "Data Analysis",
                    Icon = new SymbolIcon(SymbolRegular.DataHistogram24),
                    TargetPageType = typeof(ToolView)
                }
            };

            NavFooterMenuItems = new ObservableCollection<object>
            {
                new NavigationViewItem()
                {
                    Content = "Settings",
                    Icon = new SymbolIcon(SymbolRegular.Settings24),
                    TargetPageType = typeof(SettingView)
                }
            };
        }

        #region [ Properties (Public for UI Binding) ]

        // Text UI (Cần OnPropertyChanged thủ công trong UpdateTexts)
        public string SettingCommandText { get; private set; }
        public string HelpCommandText { get; private set; }
        public string MenuCommandText { get; private set; }

        // ViewModels con
        public SettingViewModel SettingVM { get; }
        public ToolViewModel ToolVM { get; }


        // Ngôn ngữ đang chọn (Giữ lại setter tùy chỉnh)
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                {
                    _languageService.ChangeLanguage(_selectedLanguage);
                    UpdateTexts();
                }
            }
        }

        // Văn bản cho dây chuyền sản xuất (Giữ lại setter tùy chỉnh)
        public string AssemblingText
        {
            get => _assemblingText;
            set
            {
                if (SetProperty(ref _assemblingText, value))
                {
                    UpdateMachineButtonTexts();
                }
            }
        }

        public List<int> SelectedAddresses => _selectedAddresses;

        #endregion

        #region [ Commands ]
        // Các Command khởi tạo thủ công
        public ICommand NavigateCommand { get; }
        public ICommand ChangeLanguageCommand { get; }
        public ICommand SettingCommand { get; set; }

        // Commands được tạo tự động bởi [RelayCommand]

        [RelayCommand]
        private void OpenSetting(Machine machine)
        {
            if (machine is null)
                return;

            var result = MessageBox.Show(
                $"Bạn có muốn mở SettingView cho máy {machine.Name} không?",
                "Xác nhận", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            // Truyền dữ liệu sang SettingVM
            SettingVM.SelectedMachine = machine;
            SettingVM.SelectedBaudrate = machine.Baudrate;
            SettingVM.SelectedChooseAssembling = machine.LineCode == "H" ? "Nong" : "Lanh";
            SettingVM.SelectedPort = machine.Port;
            SettingVM.NameMachine = machine.Name;
            SettingVM.AddressMachine = machine.Address.ToString();

            // Gán giá trị cho Assembling 
            if (SettingVM.SelectedAssembling == null)
                // Giả định KeyValue là một model có sẵn. 
                // Nếu nó nằm trong Electric_Meter.Core, bạn cần đảm bảo namespace đó được using
                SettingVM.SelectedAssembling = new KeyValue();

            SettingVM.SelectedAssembling.key = machine.Line;
            SettingVM.SelectedAssembling.value = SettingVM.LstAssemblings
                .FirstOrDefault(x => x.key == machine.Line)?.value;

            SettingVM.IsEnabledBtnAddMachine = false;
            SettingVM.IsEnableBtnEditMachine = true;

            //CurrentViewModel = SettingVM;
            _navigationService?.Navigate(typeof(SettingView));
        }

        [RelayCommand]
        private void OpenTool(Machine machine)
        {
            if (machine is null)
                return;

            ToolVM.AddressCurrent = machine.Address;
            ToolVM.IdMachine = machine.Id;
            ToolVM.StartTimer();
            _navigationService?.Navigate(typeof(ToolView));
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
        }

        private void ExecuteSettingForm(object parameter)
        {
            _navigationService?.Navigate(typeof(SettingView));
        }

        private void ChangeLanguage(object languageCode)
        {
            if (languageCode is string code)
                LoadLanguage(code);
        }
        #endregion

        #region [ Language & Text Update ]
        private void UpdateTexts()
        {
            SettingCommandText = _languageService.GetString("Settings");
            HelpCommandText = _languageService.GetString("Helps");
            MenuCommandText = _languageService.GetString("Menu");

            // Cập nhật text cho SettingVM
            SettingVM.NameMachineCommandText = _languageService.GetString("Name");
            SettingVM.ConnectCommandText = _languageService.GetString("Connect");
            SettingVM.BaudrateMachineCommandText = _languageService.GetString("Baudrate");
            SettingVM.PortMachineCommandText = _languageService.GetString("Port");
            SettingVM.AddressMachineCommandText = _languageService.GetString("Address");
            SettingVM.AddMachineCommandText = _languageService.GetString("Add Machine");
            SettingVM.EditMachineCommandText = _languageService.GetString("Edit Machine");
            SettingVM.DeleteMachineCommandText = _languageService.GetString("Delete Machine");

            // Cập nhật UI
            OnPropertyChanged(nameof(SettingCommandText));
            OnPropertyChanged(nameof(HelpCommandText));
            OnPropertyChanged(nameof(MenuCommandText));
        }

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
        #endregion

        #region [ Machine Management ]
        private void OnNewButtonCreated(System.Windows.Controls.Button factoryButton, System.Windows.Controls.Button assemblingButton)
        {
            var newMachine = new Machine
            {
                Name = factoryButton.Content.ToString(),
                Line = assemblingButton.Content.ToString()
            };
            Machines.Add(newMachine);
        }

        public void LoadDefaultMachine()
        {
            Machines.Clear();
            var machinesFromDb = _context.machines.ToList();
            foreach (var machine in machinesFromDb)
                Machines.Add(machine);
        }

        private void UpdateMachineButtonTexts()
        {
            // Logic cũ: AssemblingText += $" {button.Line}";
            // Cần xem lại logic này, vì nó chỉ thêm text mà không reset, 
            // có thể dẫn đến lặp lại. Giữ nguyên theo code gốc nhưng cần lưu ý.
            foreach (var button in Machines)
                AssemblingText += $" {button.Line}";
        }
        #endregion
    }
}
