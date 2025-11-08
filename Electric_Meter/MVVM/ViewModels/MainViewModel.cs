using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;

using Electric_Meter.Configs;
using Electric_Meter.Core;
using Electric_Meter.Models;
using Electric_Meter.Services;

using Newtonsoft.Json;

using Machine = Electric_Meter.Models.Machine;

namespace Electric_Meter.MVVM.ViewModels
{
    public class MainViewModel : ObservableObject, INotifyPropertyChanged
    {

        private readonly AppSetting _appSetting;
        private readonly PowerTempWatchContext _context;
        private readonly LanguageService _languageService;
        // Các ViewModel con
        public SettingViewModel SettingVM { get; }
        public ToolViewModel ToolVM { get; }


        // Navigation Command
        public ICommand NavigateCommand { get; }
        // Constructor
        public MainViewModel(SettingViewModel settingViewModel, ToolViewModel toolViewModel, AppSetting appSetting, PowerTempWatchContext powerTempWatchContext)
        {
            _context = powerTempWatchContext;
            SettingVM = settingViewModel;
            ToolVM = toolViewModel;
            CurrentViewModel = SettingVM;

            // Listen to the NewButtonCreated event from SettingViewModel
            SettingVM.NewButtonCreated += OnNewButtonCreated;

            Machines = new ObservableCollection<Machine>();

            _appSetting = appSetting;
            CurrentFactory = _appSetting.CurrentArea;

            //Hiển thị DeviceConfig
            DeviceConfig = new DeviceConfig();


            // Lệnh mở SettingView
            SettingCommand = new RelayCommand(ExecuteSettingForm);


            // Đăng ký sự kiện
            SettingVM.OnMachineLoadDefault += LoadDefaultMachine;
            //Khai báo Language Service
            _languageService = new LanguageService();
            UpdateTexts();
            LoadLanguage("en"); // Mặc định là tiếng Anh


            ChangeLanguageCommand = new RelayCommand(ChangeLanguage);

            LoadDefaultMachine();
            ToolVM.Start();


        }
        private Dictionary<string, string> _currentLanguage;
        public ICommand ChangeLanguageCommand { get; }
        public void LoadLanguage(string languageCode)
        {
            string filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), $"Languages/{languageCode}.json");
            if (File.Exists(filePath))
            {
                var jsonData = File.ReadAllText(filePath);
                _currentLanguage = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
                OnPropertyChanged(""); // Thông báo tất cả các thuộc tính thay đổi để cập nhật UI
            }
        }
        public string SettingCommandText { get; private set; }
        public string HelpCommandText { get; private set; }
        public string MenuCommandText { get; private set; }
        private void UpdateTexts()
        {
            SettingCommandText = _languageService.GetString("Settings");
            HelpCommandText = _languageService.GetString("Helps");
            MenuCommandText = _languageService.GetString("Menu");


            //SettingViewModel
            SettingVM.NameMachineCommandText = _languageService.GetString("Name");
            SettingVM.ConnectCommandText = _languageService.GetString("Connect");
            SettingVM.BaudrateMachineCommandText = _languageService.GetString("Baudrate");
            SettingVM.PortMachineCommandText = _languageService.GetString("Port");
            SettingVM.AddressMachineCommandText = _languageService.GetString("Address");
            SettingVM.AddMachineCommandText = _languageService.GetString("Add Machine");
            SettingVM.EditMachineCommandText = _languageService.GetString("Edit Machine");
            SettingVM.DeleteMachineCommandText = _languageService.GetString("Delete Machine");




            OnPropertyChanged(nameof(SettingCommandText));
            OnPropertyChanged(nameof(HelpCommandText));
            OnPropertyChanged(nameof(MenuCommandText));
            OnPropertyChanged(nameof(AssemblingText));

            OnPropertyChanged(nameof(SettingVM.NameMachineCommandText));
            OnPropertyChanged(nameof(SettingVM.ConnectCommandText));
            OnPropertyChanged(nameof(SettingVM.AddressMachineCommandText));
            OnPropertyChanged(nameof(SettingVM.AddMachineCommandText));
            OnPropertyChanged(nameof(SettingVM.EditMachineCommandText));
            OnPropertyChanged(nameof(SettingVM.DeleteMachineCommandText));
            OnPropertyChanged(nameof(SettingVM.BaudrateMachineCommandText));
            OnPropertyChanged(nameof(SettingVM.PortMachineCommandText));
            OnPropertyChanged(nameof(SettingVM.NameMachineCommandText));


        }
        private void ChangeLanguage(object languageCode)
        {
            if (languageCode is string code)
            {
                LoadLanguage(code);
            }
        }

        // Commands
        public ICommand SettingCommand { get; set; }

        #region Entity

        private UserControl _currentView;
        public UserControl CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        private string _selectedLanguage;
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged(nameof(SelectedLanguage));
                    _languageService.ChangeLanguage(_selectedLanguage);
                    UpdateTexts(); // Cập nhật lại văn bản
                }
            }
        }

        private string _assemblingText;
        public string AssemblingText
        {
            get => _assemblingText;
            set
            {
                _assemblingText = value;
                OnPropertyChanged(nameof(AssemblingText));
                UpdateMachineButtonTexts();  // Gọi hàm cập nhật button khi AssemblingText thay đổi
            }
        }

        // Hàm để cập nhật lại text của các button
        private void UpdateMachineButtonTexts()
        {
            foreach (var button in Machines)
            {
                AssemblingText = AssemblingText + " " + button.Line;  // Cập nhật nội dung button
            }
        }
        private string _currentFactory;
        public string CurrentFactory
        {
            get => _currentFactory;
            set
            {
                if (_currentFactory != value)
                {
                    _currentFactory = value;
                    OnPropertyChanged(nameof(CurrentFactory));
                }
            }
        }

        private void ExecuteSettingForm(object parameter)
        {
            CurrentViewModel = SettingVM;  // Switch to the SettingViewModel
        }
        #endregion

        #region ObservableCollection for Machines
        public ObservableCollection<object> NavigationItems { get; set; }
        public ObservableCollection<object> FooterNavigationItems { get; set; }
        private ObservableCollection<Machine> _machines;
        public ObservableCollection<Machine> Machines
        {
            get { return _machines; }
            set
            {
                _machines = value;
                OnPropertyChanged(nameof(Machines));
            }
        }
        #endregion

        #region Current ViewModel
        private BaseViewModel _currentViewModel;
        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }
        #endregion

        #region Event to handle new button creation
        private void OnNewButtonCreated(Button factoryButton, Button assemblingButton)
        {
            // Create a new Machine object using the information from the buttons
            var newMachine = new Machine
            {
                Name = factoryButton.Content.ToString(),
                Line = assemblingButton.Content.ToString()
            };

            // Add the new machine to the Machines collection
            Machines.Add(newMachine);
        }
        #endregion

        #region Load machines from the database
        public void LoadDefaultMachine()
        {
            Machines.Clear();
            var machinesFromDb = _context.machines.ToList(); // Get list of machines from the database

            foreach (var machine in machinesFromDb)
            {
                Machines.Add(machine);
            }

            // Quay lại MainView nếu cần

        }
        #endregion

        #region Mouse Click Event Commands
        public ICommand OpenSettingCommand => new RelayCommand(parameter =>
        {
            // Lấy đối tượng Machine từ CommandParameter
            var machine = parameter as Machine;

            if (machine == null)
                return;

            // Hiển thị MessageBox xác nhận
            var result = MessageBox.Show($"Bạn có muốn mở SettingView cho máy {machine.Name} không?",
                                         "Xác nhận",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Truyền dữ liệu từ Machine sang SettingViewModel
                SettingVM.SelectedMachine = machine;
                SettingVM.SelectedBaudrate = machine.Baudrate; // Ví dụ thuộc tính Baudrate
                if (SettingVM.SelectedAssembling == null)
                {
                    SettingVM.SelectedAssembling = new KeyValue(); // Khởi tạo mới
                }


                SettingVM.SelectedChooseAssembling = machine.LineCode == "H" ? "Nong" : "Lanh";
                SettingVM.SelectedPort = machine.Port;
                SettingVM.NameMachine = machine.Name;
                SettingVM.AddressMachine = machine.Address.ToString();
                SettingVM.SelectedAssembling.key = machine.Line;
                SettingVM.SelectedAssembling.value = SettingVM.LstAssemblings.Where(x => x.key == machine.Line).Select(x => x.value).ToString();

                SettingVM.IsEnabledBtnAddMachine = false;
                SettingVM.IsEnableBtnEditMachine = true;


                // Chuyển sang SettingView
                CurrentViewModel = SettingVM;
            }
        });

        private List<int> _selectedAddresses = new List<int>();  // Danh sách địa chỉ đã chọn

        public List<int> SelectedAddresses
        {
            get { return _selectedAddresses; }
        }


        // Dictionary để quản lý các Task theo địa chỉ
        private readonly Dictionary<int, CancellationTokenSource> _addressTasks = new Dictionary<int, CancellationTokenSource>();

        public ICommand OpenToolCommand => new RelayCommand(parameter =>
        {
            if (parameter is Machine machine)
            {
                ToolVM.AddressCurrent = machine.Address;
                //// Kiểm tra xem địa chỉ đã có trong danh sách hay chưa
                //if (!_addressTasks.ContainsKey(machine.Address))
                //{
                //    // Tạo CancellationTokenSource cho địa chỉ mới
                //    var cancellationTokenSource = new CancellationTokenSource();
                //    _addressTasks[machine.Address] = cancellationTokenSource;

                //    // Tạo Task xử lý địa chỉ trong vòng lặp liên tục
                //    try
                //    {
                //        // Chuyển Address thành danh sách


                //    }
                //    catch (OperationCanceledException ex)
                //    {
                //        MessageBox.Show(ex.Message);
                //    }
                //}
                //else
                //{
                //    // Nếu task đã tồn tại, có thể thông báo cho người dùng hoặc bỏ qua
                //    Tool.Log($"Địa chỉ {machine.Address} đã có task đang chạy.");
                //}

                // Thiết lập máy và chuyển ViewModel
                ToolVM.IdMachine = machine.Id;
                CurrentViewModel = ToolVM;
                ToolVM.StartTimer();
            }
        });



        #endregion
        private DeviceConfig _deviceConfig;

        public DeviceConfig DeviceConfig
        {
            get { return _deviceConfig; }
            set
            {
                _deviceConfig = value;
                OnPropertyChanged(nameof(DeviceConfig));
            }
        }

        private void HandleDeviceConfigMessage(DeviceConfig message)
        {
            if (message != null)
            {
                DeviceConfig = message;
                ToolVM.Port = message.Port;
                ToolVM.Baudrate = message.Baudrate;
                //ToolVM.Address = message.AddressMachine;

            }
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}
