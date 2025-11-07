using Electric_Meter.Core;
using Electric_Meter.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using Electric_Meter.Services;
using Electric_Meter.Configs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Windows.Media;

namespace Electric_Meter.MVVM.ViewModels
{
    public class SettingViewModel :BaseViewModel
    {
        private readonly Service _iService;
        private readonly ToolViewModel _toolViewModel;
        private readonly AppSetting _appSetting;
        private readonly PowerTempWatchContext _powerTempWatchContext;
        public event Action OnMachineLoadDefault;
        //Constructor
        public SettingViewModel(Service service, ToolViewModel toolViewModel, AppSetting appSetting, PowerTempWatchContext powerTempWatchContext)
        {
            IsEnabledBtnConnect = true;
            IsEnabledBtnAddMachine = true;
            IsEnableBtnEditMachine = false;
            _appSetting = appSetting;

            _toolViewModel = toolViewModel;

            _toolViewModel.Port = _appSetting.Port;
            _toolViewModel.Baudrate = _appSetting.Baudrate;
            // list baudrate

            LstBaudrate = new ObservableCollection<int>()
            {
                9600,14400,19200,2400,1200
            };

            LstChooseAssembling = new ObservableCollection<string>()
            {
                "Nong","Lanh"
            };
            _powerTempWatchContext = powerTempWatchContext;


            // Lấy danh sách lstAssembling từ cơ sở dữ liệu
            List<string> lstAssembling = _powerTempWatchContext.dvFactoryAssemblings
                .Where(x => x.Factory == _appSetting.CurrentArea)
                .Select(x => x.Assembling)
                .ToList();

            // Khởi tạo danh sách lstAssemblings
            LstAssemblings = new List<KeyValue>();

            // Thêm từng mục vào danh sách lstAssemblings
            foreach (var item in lstAssembling)
            {
                LstAssemblings.Add(new KeyValue
                {
                    key = item,
                    value = "Thành Hình " + item
                });
            }

            
            _iService = service;

            GetPorts();

            ConnectCommand = new RelayCommand(ExecuteConnectCommand, CanConnect);
            AddMachineCommand = new RelayCommand(ExecuteAddMachineCommand, CanAddMachine);
            EditMachineCommand = new RelayCommand(ExecuteEditMachineCommand, CanEditMachine);
            DeleteMachineCommand = new RelayCommand(ExecuteDeleteMachineCommand, CanDeleteMachine);
            //_toolViewModel.Start();//tự động chạy app
        }
        #region Command
        public ICommand ConnectCommand { get; set; }
        public ICommand AddMachineCommand { get; set; }
        public ICommand EditMachineCommand { get; set; }
        public ICommand DeleteMachineCommand { get; set; }

        #endregion
        #region Entity


        public event Action<Button,Button> NewButtonCreated;
        

        public ObservableCollection<int> LstBaudrate { get; set; }
        public ObservableCollection<string> LstChooseAssembling { get; set; }
        

        private List<KeyValue> _lstAssebling;
        public List<KeyValue> LstAssemblings
        {
            get { return _lstAssebling; }
            set
            {
                _lstAssebling = value;
                OnPropertyChanged(nameof(LstAssemblings));
            }
        }
        private KeyValue _selectedAssembling;
        public KeyValue SelectedAssembling
        {
            get { return _selectedAssembling; }
            set
            {
                _selectedAssembling = value;
                OnPropertyChanged(nameof(SelectedAssembling));
            }
        }

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

        private Machine _selectedMachine;
        public Machine SelectedMachine
        {
            get => _selectedMachine;
            set
            {
                if (_selectedMachine != value)
                {
                    _selectedMachine = value;
                    OnPropertyChanged(nameof(SelectedMachine));
                }
            }
        }
        // Thuộc tính lưu trữ Baudrate được chọn
        private int _selectedBaudrate;
        public int SelectedBaudrate
        {
            get => _selectedBaudrate;
            set
            {
                if (_selectedBaudrate != value)
                {
                    _selectedBaudrate = value;
                    OnPropertyChanged(nameof(SelectedBaudrate));
                }
            }
        }

        


        //Thuoc tinh luu tru port
        private string _selectPort;

        public string SelectedPort
        {
            get => _selectPort;
            set
            {
                if (_selectPort != value)
                {
                    _selectPort = value;
                    OnPropertyChanged(nameof(SelectedPort));
                }
            }
        }


        //Thuoc tinh luu tru LstAssembling
        private string _selectedChooseAssembling;

        public string SelectedChooseAssembling
        {
            get => _selectedChooseAssembling;
            set
            {
                if (_selectedChooseAssembling != value)
                {

                    _selectedChooseAssembling = value;
                    OnPropertyChanged(nameof(SelectedChooseAssembling));
                }
            }
        }


        private string _nameMachine;
        public string NameMachine
        {
            get => _nameMachine;
            set
            {
                if (_nameMachine != value)
                {
                    // Cho phép tạm thời đặt giá trị rỗng
                    _nameMachine = value;

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        ErrorMessage = string.Empty; // Không hiển thị lỗi khi người dùng xóa toàn bộ
                    }
                    else if (!IsValidName(value.TrimStart()))
                    {
                        ErrorMessage = "NameMachine không được chứa ký tự đặc biệt.";
                    }
                    else
                    {
                        _nameMachine = value.TrimStart(); // Xóa khoảng trắng ở đầu chuỗi
                        ErrorMessage = string.Empty; // Xóa thông báo lỗi nếu hợp lệ
                    }

                    OnPropertyChanged(nameof(NameMachine));
                }
            }
        }
        // Hàm kiểm tra giá trị hợp lệ (không chứa ký tự đặc biệt)
        private bool IsValidName(string name)
        {
            // Chỉ cho phép các ký tự chữ, số và khoảng trắng
            return System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z0-9 ]+$");
        }


        //Thuoc tinh luu tru Address
        private string _addressMachine = string.Empty;
        public string AddressMachine
        {
            get => _addressMachine;
            set
            {
                if (_addressMachine != value)
                {
                    if (int.TryParse(value, out int parsedValue) && parsedValue >= 1 && parsedValue <= 50)
                    {
                        _addressMachine = value;
                        ErrorMessage = string.Empty; // Xóa lỗi nếu giá trị hợp lệ
                    }
                    else if (string.IsNullOrWhiteSpace(value))
                    {
                        _addressMachine = value; // Cho phép chuỗi rỗng
                        ErrorMessage = "Vui lòng nhập số từ 1 đến 50.";
                    }
                    else
                    {
                        ErrorMessage = "AddressMachine chỉ cho phép nhập số từ 1 đến 50.";
                    }
                    OnPropertyChanged(nameof(AddressMachine));
                }
            }
        }


        public int address;
        public int Address
        {
            get => address;
            set
            {
                this.address = value;
                OnPropertyChanged(nameof(Address));
            }
        }

        public string this[string columnName]
        {
            get
            {
                string error = null;
                switch (columnName)
                {
                    case nameof(Port):
                        if (string.IsNullOrEmpty(Port))
                            error = "Port is required.";
                        else if (Port == "COM1")
                            error = "Port is default! Choose anthore port.";
                        break;
                    case nameof(Baudrate):
                        if (string.IsNullOrEmpty(Baudrate.ToString()))
                            error = "Baudrate is required.";
                        else if (Baudrate != 115200)
                            error = "Baudrate is not correct.";
                        break;

                }
                return error;
            }
        }

        public string Error => null;


        //Thuoc tinh EnableBtn

        private bool isEnabledBtnConnect;

        public bool IsEnabledBtnConnect
        {
            get { return isEnabledBtnConnect; }
            set
            {
                isEnabledBtnConnect = value;
                OnPropertyChanged(nameof(IsEnabledBtnConnect));
            }
        }
        private bool isEnabledBtnAddMachine;

        public bool IsEnabledBtnAddMachine
        {
            get { return isEnabledBtnAddMachine; }
            set
            {
                isEnabledBtnAddMachine = value;
                OnPropertyChanged(nameof(IsEnabledBtnAddMachine));
            }
        }
        private bool isEnabledBtnEditMachine;
        public bool IsEnableBtnEditMachine
        {
            get { return isEnabledBtnEditMachine; }
            set
            {
                isEnabledBtnEditMachine = value;
                OnPropertyChanged(nameof(IsEnableBtnEditMachine));
            }
        }
        private bool isEnabledBtnDeleteMachine;
        public bool IsEnabledBtnDeleteMachine
        {
            get { return isEnabledBtnDeleteMachine; }
            set
            {
                isEnabledBtnDeleteMachine = value;
                OnPropertyChanged(nameof(IsEnabledBtnDeleteMachine));
            }
        }
        #endregion
        #region Khai báo và lấy ra danh sách các post

        private List<string> _lstPost;
        public List<string> ListPost
        {
            get => _lstPost;
            set
            {

                this._lstPost = value;
                OnPropertyChanged(nameof(_lstPost));
            }
        }

        public string port;
        public string Port
        {
            get => port;
            set
            {
                this.port = value;
                OnPropertyChanged(nameof(_lstPost));
            }
        }
        public void GetPorts()
        {
            string[] ArryPort = SerialPort.GetPortNames();
            ListPost = ArryPort.ToList<string>();
        }
        #endregion

        #region Lấy ra danh sách các tốc độ truyền

        private List<int> _lstBaute;
        public List<int> lstBaute
        {
            get => _lstBaute;
            set
            {
                this._lstBaute = value;
                OnPropertyChanged(nameof(lstBaute));
            }
        }
        public int baudrate;
        public int Baudrate
        {
            get => baudrate;
            set
            {
                this.baudrate = value;
                OnPropertyChanged(nameof(Baudrate));
            }
        }
        #endregion

        #region GetPortName
        public ObservableCollection<string> LstPort { get; set; } = new ObservableCollection<string>();


        public void GetPortName()
        {
            string[] lstPort = SerialPort.GetPortNames();
            foreach (var item in lstPort)
            {
                LstPort.Add(item);

            }
        }
        #endregion
        #region Language
        private string _connectCommandText;
        public string ConnectCommandText
        {
            get => _connectCommandText;
            set
            {
                _connectCommandText = value;
                OnPropertyChanged(nameof(ConnectCommandText));
            }
        }
        private string _addMachineCommandText;
        public string AddMachineCommandText
        {
            get => _addMachineCommandText;
            set
            {
                _addMachineCommandText = value;
                OnPropertyChanged(nameof(AddMachineCommandText));
            }
        }
        private string _editMachineCommandText;
        public string EditMachineCommandText
        {
            get => _editMachineCommandText;
            set
            {
                _editMachineCommandText = value;
                OnPropertyChanged(nameof(EditMachineCommandText));
            }
        }
        private string _deleteMachineCommandText;
        public string DeleteMachineCommandText
        {
            get => _deleteMachineCommandText;
            set
            {
                _deleteMachineCommandText = value;
                OnPropertyChanged(nameof(DeleteMachineCommandText));
            }
        }

        private string _addressMachineCommandText;
        public string AddressMachineCommandText
        {
            get => _addressMachineCommandText;
            set
            {
                _addressMachineCommandText = value;
                OnPropertyChanged(nameof(AddressMachineCommandText));
            }
        }

        private string _baudrateMachineCommandText;
        public string BaudrateMachineCommandText
        {
            get => _baudrateMachineCommandText;
            set
            {
                _baudrateMachineCommandText = value;
                OnPropertyChanged(nameof(BaudrateMachineCommandText));
            }
        }


        private string _nameMachineCommandText;
        public string NameMachineCommandText
        {
            get => _nameMachineCommandText;
            set
            {
                _nameMachineCommandText = value;
                OnPropertyChanged(nameof(NameMachineCommandText));
            }
        }
        private string _portMachineCommandText;
        public string PortMachineCommandText
        {
            get => _portMachineCommandText;
            set
            {
                _portMachineCommandText = value;
                OnPropertyChanged(nameof(PortMachineCommandText));
            }
        }

        #endregion
        public DeviceConfig message = new DeviceConfig();
        //Connect
        public async void ExecuteConnectCommand(object parameter)
        {
            //if (string.IsNullOrWhiteSpace(Port) || !DataModelConstant.BaudrateConst.Contains(Baudrate))
            //{
            //    MessageBox.Show("Please connect to the device before");
            //    return;
            //}
            //if (Port == "COM1" || Baudrate != 2400)
            //{
            //    MessageBox.Show("Please choose correct connection");
            //    return;
            //}

            try
            {
                //message.Port = this.Port;
                //message.Baudrate = this.Baudrate;


                message.Port = _appSetting.Port;
                message.Factory = _appSetting.CurrentArea;
                
                // set port for _toolViewModel


                _toolViewModel.Start();
                IsEnabledBtnConnect = false;
                MessageBox.Show("Connection successful!");

                

            }
            catch (Exception ex)
            {
                IsEnabledBtnConnect = true;
                MessageBox.Show("Connection erro!" + ex.Message);
            }
        }
        private bool CanConnect(object parameter)
        {
            return IsEnabledBtnConnect;
        }
        //Add Machine
        public async void ExecuteAddMachineCommand(object parameter)
        {

            try
            {
                
                if (_powerTempWatchContext.machines.Any(x => x.Name == NameMachine))
                {
                    MessageBox.Show("Machine is allready!");
                    return;
                }
                Machine machines = new Machine();
                machines.Name = NameMachine;
                machines.Port = SelectedPort;
                machines.Baudrate = SelectedBaudrate;
                machines.Address = int.Parse(AddressMachine);
                machines.Line = SelectedAssembling.key;
                machines.LineCode = SelectedChooseAssembling == "Nong" ? "H" : "C";

                await _iService.InsertToMachine(machines);
                IsEnabledBtnAddMachine = false;

                // Tạo nút Machine
                Button btn_Machine = new Button
                {
                    Content = NameMachine,
                    Background = SelectedChooseAssembling == "Nong" ? Brushes.White : Brushes.Blue
                };

                // Tạo nút Assembling
                Button btn_Assembling = new Button
                {
                    Content = SelectedAssembling.value
                };

                // Gửi Button qua sự kiện
                NewButtonCreated?.Invoke(btn_Machine, btn_Assembling);
            }
            catch (Exception ex)
            {

                IsEnabledBtnAddMachine = true;
                MessageBox.Show("Add machine errors: " + ex.Message);
            }
            
        }
        private bool CanAddMachine(object parameter)
        {
            try
            {
                return 
               !string.IsNullOrEmpty(AddressMachine.ToString()) &&
               !string.IsNullOrEmpty(NameMachine) &&
               !string.IsNullOrEmpty(SelectedPort) &&
               !string.IsNullOrEmpty(SelectedBaudrate.ToString()) &&
               !string.IsNullOrEmpty(SelectedAssembling?.value) &&
               !string.IsNullOrEmpty(SelectedChooseAssembling);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        //Edit
        public async void ExecuteEditMachineCommand(object parameter)
        {
            try
            {
                if (!IsEnableBtnEditMachine)
                {
                    MessageBox.Show("Button is disabled. Cannot edit machine.");
                    return;
                }

                if (SelectedMachine == null)
                {
                    MessageBox.Show("No machine selected.");
                    return;
                }

                // Tìm máy trong cơ sở dữ liệu
                var find = await _powerTempWatchContext.machines.FirstOrDefaultAsync(x => x.Id == SelectedMachine.Id);

                if (find == null)
                {
                    MessageBox.Show("Machine not found.");
                    return;
                }

                // Cập nhật thuộc tính của máy
                find.Address = int.Parse(AddressMachine);
                find.Port = SelectedPort;
                find.Baudrate = SelectedBaudrate;
                find.Name = NameMachine;
                find.Line = SelectedAssembling.key;
                find.LineCode = SelectedChooseAssembling == "Nong" ? "H" : "C";

                // Lưu thay đổi vào cơ sở dữ liệu
                await _iService.EditToMachine(find);
                OnMachineLoadDefault?.Invoke();
                MessageBox.Show("Edit successfully!");
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private bool CanEditMachine(object parameter)
        {
            try
            {
                return !string.IsNullOrEmpty(AddressMachine.ToString()) &&
               !string.IsNullOrEmpty(NameMachine) &&
               !string.IsNullOrEmpty(SelectedPort) &&
               !string.IsNullOrEmpty(SelectedBaudrate.ToString()) &&
               !string.IsNullOrEmpty(SelectedAssembling?.value) &&
               !string.IsNullOrEmpty(SelectedChooseAssembling);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        //Delete
        public async void ExecuteDeleteMachineCommand(object parameter)
        {
            try
            {
                if (!IsEnableBtnEditMachine)
                {
                    MessageBox.Show("Button is disabled. Cannot edit machine.");
                    return;
                }

                if (SelectedMachine == null)
                {
                    MessageBox.Show("No machine selected.");
                    return;
                }

                // Tìm máy trong cơ sở dữ liệu
                var find = await _powerTempWatchContext.machines.FirstOrDefaultAsync(x => x.Id == SelectedMachine.Id);

                if (find == null)
                {
                    MessageBox.Show("Machine not found.");
                    return;
                }

                

                // Lưu thay đổi vào cơ sở dữ liệu
                await _iService.DeleteToMachine(find);
                // Giả sử đã xóa thành công
                OnMachineLoadDefault?.Invoke();
                MessageBox.Show("Delete successfully!");
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }
        private bool CanDeleteMachine(object parameter)
        {
            try
            {
                return !string.IsNullOrEmpty(AddressMachine.ToString()) &&
                int.Parse(AddressMachine) >= 1 &&
                int.Parse(AddressMachine) <= 50 &&
               !string.IsNullOrEmpty(NameMachine) &&
               !string.IsNullOrEmpty(SelectedPort) &&
               !string.IsNullOrEmpty(SelectedBaudrate.ToString()) &&
               !string.IsNullOrEmpty(SelectedAssembling?.value) &&
               !string.IsNullOrEmpty(SelectedChooseAssembling);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged(nameof(ErrorMessage));
                }
            }
        }
    }
}
