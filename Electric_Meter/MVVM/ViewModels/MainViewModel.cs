using Electric_Meter.Configs;
using Electric_Meter.Core;
using Electric_Meter.Interfaces;
using Electric_Meter.Models;
using Electric_Meter.Services;
using Electric_Meter.Utilities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Machine = Electric_Meter.Models.Machine;

namespace Electric_Meter.MVVM.ViewModels
{
    public class MainViewModel : BaseViewModel, INotifyPropertyChanged
    {
        private readonly SettingViewModel _settingViewModel;
        private readonly AppSetting _appSetting;
        private readonly PowerTempWatchContext _context;
        private readonly ToolViewModel _toolViewModel;
        private readonly LanguageService _languageService;
        // Constructor
        public MainViewModel(SettingViewModel settingViewModel, ToolViewModel toolViewModel, AppSetting appSetting, PowerTempWatchContext powerTempWatchContext)
        {
            _context = powerTempWatchContext;
            _settingViewModel = settingViewModel;
            _toolViewModel = toolViewModel;
            CurrentViewModel = _settingViewModel;

            // Listen to the NewButtonCreated event from SettingViewModel
            _settingViewModel.NewButtonCreated += OnNewButtonCreated;

            Machines = new ObservableCollection<Machine>();

            _appSetting = appSetting;
            CurrentFactory = _appSetting.CurrentArea;

            //Hiển thị DeviceConfig
            DeviceConfig = new DeviceConfig();
            

            // Lệnh mở SettingView
            SettingCommand = new RelayCommand(ExecuteSettingForm);


            // Đăng ký sự kiện
            _settingViewModel.OnMachineLoadDefault += LoadDefaultMachine;
            //Khai báo Language Service
            _languageService = new LanguageService();
            UpdateTexts();
            LoadLanguage("en"); // Mặc định là tiếng Anh


            ChangeLanguageCommand = new RelayCommand(ChangeLanguage);

            LoadDefaultMachine();
            _toolViewModel.Start();


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
            _settingViewModel.NameMachineCommandText = _languageService.GetString("Name");
            _settingViewModel.ConnectCommandText = _languageService.GetString("Connect");
            _settingViewModel.BaudrateMachineCommandText = _languageService.GetString("Baudrate");
            _settingViewModel.PortMachineCommandText = _languageService.GetString("Port");
            _settingViewModel.AddressMachineCommandText = _languageService.GetString("Address");
            _settingViewModel.AddMachineCommandText = _languageService.GetString("Add Machine");
            _settingViewModel.EditMachineCommandText = _languageService.GetString("Edit Machine");
            _settingViewModel.DeleteMachineCommandText = _languageService.GetString("Delete Machine");
            



            OnPropertyChanged(nameof(SettingCommandText));
            OnPropertyChanged(nameof(HelpCommandText));
            OnPropertyChanged(nameof(MenuCommandText));
            OnPropertyChanged(nameof(AssemblingText));

            OnPropertyChanged(nameof(_settingViewModel.NameMachineCommandText));
            OnPropertyChanged(nameof(_settingViewModel.ConnectCommandText));
            OnPropertyChanged(nameof(_settingViewModel.AddressMachineCommandText));
            OnPropertyChanged(nameof(_settingViewModel.AddMachineCommandText));
            OnPropertyChanged(nameof(_settingViewModel.EditMachineCommandText));
            OnPropertyChanged(nameof(_settingViewModel.DeleteMachineCommandText));
            OnPropertyChanged(nameof(_settingViewModel.BaudrateMachineCommandText));
            OnPropertyChanged(nameof(_settingViewModel.PortMachineCommandText));
            OnPropertyChanged(nameof(_settingViewModel.NameMachineCommandText));
            
            
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
            CurrentViewModel = _settingViewModel;  // Switch to the SettingViewModel
        }
        #endregion

        #region ObservableCollection for Machines
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
                _settingViewModel.SelectedMachine = machine;
                _settingViewModel.SelectedBaudrate = machine.Baudrate; // Ví dụ thuộc tính Baudrate
                if (_settingViewModel.SelectedAssembling == null)
                {
                    _settingViewModel.SelectedAssembling = new KeyValue(); // Khởi tạo mới
                }
                

                _settingViewModel.SelectedChooseAssembling = machine.LineCode == "H" ? "Nong" : "Lanh";
                _settingViewModel.SelectedPort = machine.Port;
                _settingViewModel.NameMachine = machine.Name;
                _settingViewModel.AddressMachine = machine.Address.ToString();
                _settingViewModel.SelectedAssembling.key = machine.Line;
                _settingViewModel.SelectedAssembling.value = _settingViewModel.LstAssemblings.Where(x => x.key == machine.Line).Select(x => x.value).ToString();
                
                _settingViewModel.IsEnabledBtnAddMachine = false;
                _settingViewModel.IsEnableBtnEditMachine = true;


                // Chuyển sang SettingView
                CurrentViewModel = _settingViewModel;
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
                _toolViewModel.AddressCurrent = machine.Address;
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
                _toolViewModel.IdMachine = machine.Id;
                CurrentViewModel = _toolViewModel;
                _toolViewModel.StartTimer();
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
                _toolViewModel.Port = message.Port;
                _toolViewModel.Baudrate = message.Baudrate;
                //_toolViewModel.Address = message.AddressMachine;

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
