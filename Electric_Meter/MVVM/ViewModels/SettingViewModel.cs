using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // Quan trọng: Cung cấp [RelayCommand]

using Electric_Meter.Configs;
using Electric_Meter.Models;
using Electric_Meter.Services;

using Microsoft.EntityFrameworkCore;

// Lưu ý: Đảm bảo rằng lớp RelayCommand cũ trong Electric_Meter.Core đã được loại bỏ 
// hoặc bạn đã xóa using Electric_Meter.Core; để tránh xung đột.

namespace Electric_Meter.MVVM.ViewModels
{
    public partial class SettingViewModel : ObservableObject
    {
        #region [ Fields - Private Dependencies ]
        private readonly Service _service;
        private readonly ToolViewModel _toolViewModel;
        private readonly AppSetting _appSetting;
        private readonly PowerTempWatchContext _context;
        #endregion

        #region [ Events ]
        public event Action OnMachineLoadDefault;
        public event Action<Button, Button> NewButtonCreated;
        #endregion

        #region [ Constructor ]
        public SettingViewModel(Service service, ToolViewModel toolViewModel, AppSetting appSetting, PowerTempWatchContext context)
        {
            _service = service;
            _toolViewModel = toolViewModel;
            _appSetting = appSetting;
            _context = context;

            // Khởi tạo trạng thái ban đầu
            // Các thuộc tính [ObservableProperty] có giá trị mặc định là true/false
            IsEnabledBtnConnect = true;
            IsEnabledBtnAddMachine = true;
            IsEnableBtnEditMachine = false;
            IsEnabledBtnDeleteMachine = true; // Mặc định là true, sẽ được kiểm soát bởi CanDelete

            // Load dữ liệu ban đầu
            LoadAssemblings();
            LoadDeviceList();
            GetPorts();

            // *** LOẠI BỎ KHỞI TẠO COMMAND THỦ CÔNG: ConnectCommand = new RelayCommand(...) ***
            // Source Generator sẽ tạo ra chúng
        }
        #endregion

        #region [ Commands ]
        // *** Đã xóa các khai báo ICommand public vì [RelayCommand] sẽ tạo chúng ***

        // public ICommand ConnectCommand { get; }
        // public ICommand AddMachineCommand { get; }
        // public ICommand EditMachineCommand { get; }
        // public ICommand DeleteMachineCommand { get; }

        #endregion

        #region [ Properties - UI State ]
        // [ObservableProperty] đã được giữ nguyên
        [ObservableProperty] private bool isEnabledBtnConnect;
        [ObservableProperty] private bool isEnabledBtnAddMachine;
        [ObservableProperty] private bool isEnableBtnEditMachine;
        [ObservableProperty] private bool isEnabledBtnDeleteMachine;
        [ObservableProperty] private string errorMessage;
        #endregion

        #region [ Properties - Machine Configuration ]
        [ObservableProperty] private string nameMachine = string.Empty;
        [ObservableProperty] private string addressMachine = string.Empty;
        [ObservableProperty] private KeyValue selectedAssembling;
        [ObservableProperty] private string selectedChooseAssembling;
        [ObservableProperty] private Machine selectedMachine;
        [ObservableProperty] private ObservableCollection<Device> deviceList = new();
        #endregion

        #region [ Properties - Communication Settings ]
        [ObservableProperty] private string selectedPort;
        [ObservableProperty] private int selectedBaudrate;
        [ObservableProperty] private ObservableCollection<string> lstPort = new();
        [ObservableProperty] private ObservableCollection<int> lstBaudrate = new();
        [ObservableProperty] private List<KeyValue> lstAssemblings = new();
        #endregion

        #region [ Language Texts ]
        [ObservableProperty] private string connectCommandText = "Connect";
        [ObservableProperty] private string addMachineCommandText = "Add Machine";
        [ObservableProperty] private string editMachineCommandText = "Edit Machine";
        [ObservableProperty] private string deleteMachineCommandText = "Delete Machine";
        [ObservableProperty] private string addressMachineCommandText = "Address";
        [ObservableProperty] private string baudrateMachineCommandText = "Baudrate";
        [ObservableProperty] private string nameMachineCommandText = "Name";
        [ObservableProperty] private string portMachineCommandText = "Port";
        #endregion

        #region [ Methods - Load & Initialization ]
        // Giữ nguyên các hàm Load
        private void LoadDeviceList()
        {
            try
            {
                var devices = _service.GetDevicesList();
                DeviceList = new ObservableCollection<Device>(devices);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading devices: " + ex.Message);
            }
        }

        private void LoadAssemblings()
        {
            try
            {
                var assemblings = _context.dvFactoryAssemblings
                    .Where(x => x.Factory == _appSetting.CurrentArea)
                    .Select(x => x.Assembling)
                    .ToList();

                LstAssemblings.Clear();
                foreach (var item in assemblings)
                {
                    LstAssemblings.Add(new KeyValue
                    {
                        key = item,
                        value = $"Thành Hình {item}"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading assemblings: " + ex.Message);
            }
        }

        private void GetPorts()
        {
            LstPort.Clear();
            foreach (var p in SerialPort.GetPortNames())
                LstPort.Add(p);

            LstBaudrate = new ObservableCollection<int> { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
        }
        #endregion

        #region [ Command Logic - Connect (Sử dụng [RelayCommand]) ]

        // [RelayCommand] tự động tạo ConnectCommand. 
        // Thêm CanExecute = nameof(CanExecuteConnect) để liên kết với logic CanConnect cũ.
        [RelayCommand(CanExecute = nameof(CanExecuteConnect))]
        private async Task ExecuteConnectCommand() // Thay đổi sang không tham số (void)
        {
            try
            {
                _toolViewModel.Start();
                IsEnabledBtnConnect = false;
                MessageBox.Show("Connection successful!");
            }
            catch (Exception ex)
            {
                IsEnabledBtnConnect = true;
                MessageBox.Show("Connection error: " + ex.Message);
            }
            finally
            {
                
            }
        }

        private bool CanExecuteConnect() => IsEnabledBtnConnect;
        #endregion

        #region [ Command Logic - Add Machine (Sử dụng [RelayCommand]) ]

        // Tự động tạo AddMachineCommand
        [RelayCommand(CanExecute = nameof(CanExecuteAddMachine))]
        private async Task ExecuteAddMachineCommand() // Thay đổi sang không tham số (void)
        {
            try
            {
                // ValidateMachineInput() đã được dùng trong CanExecute, nhưng vẫn kiểm tra lại trước khi thực hiện
                if (!ValidateMachineInput()) return;

                if (_context.machines.Any(x => x.Name == NameMachine))
                {
                    MessageBox.Show("Machine already exists!");
                    return;
                }

                var newMachine = new Machine
                {
                    Name = NameMachine.Trim(),
                    Port = SelectedPort,
                    Baudrate = SelectedBaudrate,
                    Address = int.Parse(AddressMachine),
                    Line = SelectedAssembling?.key ?? "",
                    LineCode = SelectedChooseAssembling == "Nong" ? "H" : "C"
                };

                await _service.InsertToMachine(newMachine);
                IsEnabledBtnAddMachine = false; // Tắt nút sau khi thêm thành công

                // Gửi button ra view
                var btnMachine = new Button
                {
                    Content = NameMachine,
                    Background = SelectedChooseAssembling == "Nong" ? Brushes.White : Brushes.Blue
                };
                var btnAssembling = new Button { Content = SelectedAssembling?.value ?? "Unknown" };

                NewButtonCreated?.Invoke(btnMachine, btnAssembling);
                MessageBox.Show("Machine added successfully!");
            }
            catch (Exception ex)
            {
                IsEnabledBtnAddMachine = true;
                MessageBox.Show("Add machine error: " + ex.Message);
            }
            finally
            {
                //AddMachineCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanExecuteAddMachine() => IsEnabledBtnAddMachine && ValidateMachineInput();
        #endregion

        #region [ Command Logic - Edit Machine (Sử dụng [RelayCommand]) ]

        // Tự động tạo EditMachineCommand
        [RelayCommand(CanExecute = nameof(CanExecuteEditMachine))]
        private async Task ExecuteEditMachineCommand() // Thay đổi sang không tham số (void)
        {
            try
            {
                // Kiểm tra trạng thái và SelectedMachine trước khi chạy
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

                var machine = await _context.machines.FirstOrDefaultAsync(x => x.Id == SelectedMachine.Id);
                if (machine == null)
                {
                    MessageBox.Show("Machine not found.");
                    return;
                }

                // Cập nhật thông tin máy
                machine.Address = int.Parse(AddressMachine);
                machine.Port = SelectedPort;
                machine.Baudrate = SelectedBaudrate;
                machine.Name = NameMachine;
                machine.Line = SelectedAssembling?.key ?? "";
                machine.LineCode = SelectedChooseAssembling == "Nong" ? "H" : "C";

                await _service.EditToMachine(machine);
                OnMachineLoadDefault?.Invoke();
                MessageBox.Show("Edit successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Edit error: " + ex.Message);
            }
        }

        private bool CanExecuteEditMachine() => IsEnableBtnEditMachine && ValidateMachineInput();
        #endregion

        #region [ Command Logic - Delete Machine (Sử dụng [RelayCommand]) ]

        // Tự động tạo DeleteMachineCommand
        [RelayCommand(CanExecute = nameof(CanExecuteDeleteMachine))]
        private async Task ExecuteDeleteMachineCommand() // Thay đổi sang không tham số (void)
        {
            try
            {
                // Kiểm tra trạng thái và SelectedMachine trước khi chạy
                if (!IsEnableBtnEditMachine) // Sử dụng IsEnableBtnEditMachine để kiểm soát Delete theo logic gốc
                {
                    MessageBox.Show("Button is disabled. Cannot delete machine.");
                    return;
                }

                if (SelectedMachine == null)
                {
                    MessageBox.Show("No machine selected.");
                    return;
                }

                var machine = await _context.machines.FirstOrDefaultAsync(x => x.Id == SelectedMachine.Id);
                if (machine == null)
                {
                    MessageBox.Show("Machine not found.");
                    return;
                }

                await _service.DeleteToMachine(machine);
                OnMachineLoadDefault?.Invoke();
                MessageBox.Show("Delete successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete error: " + ex.Message);
            }
        }

        private bool CanExecuteDeleteMachine() => IsEnableBtnEditMachine && SelectedMachine != null; // Chỉ xóa khi đang ở chế độ Edit và có máy được chọn
        #endregion

        #region [ Helper / Validation ]
        // Giữ nguyên hàm ValidateMachineInput
        private bool ValidateMachineInput()
        {
            if (string.IsNullOrWhiteSpace(NameMachine))
            {
                ErrorMessage = "NameMachine is required.";
                return false;
            }

            if (!Regex.IsMatch(NameMachine, @"^[a-zA-Z0-9 ]+$"))
            {
                ErrorMessage = "NameMachine cannot contain special characters.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(AddressMachine) ||
                !int.TryParse(AddressMachine, out int addr) || addr < 1 || addr > 50)
            {
                ErrorMessage = "AddressMachine must be a number between 1 and 50.";
                return false;
            }

            if (string.IsNullOrEmpty(SelectedPort) ||
                SelectedBaudrate == 0 ||
                SelectedAssembling == null ||
                string.IsNullOrEmpty(SelectedChooseAssembling))
            {
                ErrorMessage = "Please fill all required fields.";
                return false;
            }

            ErrorMessage = string.Empty;
            return true;
        }
        #endregion
    }
}
