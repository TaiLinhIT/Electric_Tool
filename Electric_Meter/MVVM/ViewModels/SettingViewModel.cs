using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

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
        private readonly LanguageService _languageService;
        private readonly Service _service;
        private readonly ToolViewModel _toolViewModel;
        private readonly AppSetting _appSetting;
        private readonly PowerTempWatchContext _context;
        #endregion

        #region [ Events ]
        public event Action OnDeviceLoadDefault;
        public event Action<Button, Button> NewButtonCreated;
        #endregion

        #region [ Constructor ]
        public SettingViewModel(LanguageService languageService, Service service, ToolViewModel toolViewModel, AppSetting appSetting, PowerTempWatchContext context)
        {
            _languageService = languageService;
            _languageService.LanguageChanged += UpdateTexts;

            UpdateTexts();
            _service = service;
            _toolViewModel = toolViewModel;
            _appSetting = appSetting;
            _context = context;

            // Khởi tạo trạng thái ban đầu
            // Các thuộc tính [ObservableProperty] có giá trị mặc định là true/false
            IsEnabledBtnConnect = true;
            IsEnabledBtnAddDevice = true;
            IsEnableBtnEditDevice = false;
            IsEnabledBtnDeleteDevice = true; // Mặc định là true, sẽ được kiểm soát bởi CanDelete

            // Load dữ liệu ban đầu
            //LoadAssemblings();
            LoadDeviceList();
            GetDefaultSetting();

        }
        #endregion



        #region [ Properties - UI State ]
        // [ObservableProperty] đã được giữ nguyên
        [ObservableProperty] private bool isEnabledBtnConnect;
        [ObservableProperty] private bool isEnabledBtnAddDevice;
        [ObservableProperty] private bool isEnableBtnEditDevice;
        [ObservableProperty] private bool isEnabledBtnDeleteDevice;
        [ObservableProperty] private string errorMessage;
        [ObservableProperty] private Device selectedDevice;

        #endregion

        #region [ Properties - Device Configuration ]
        [ObservableProperty] private string nameDevice = string.Empty;
        [ObservableProperty] private int addressDevice;
        [ObservableProperty] private KeyValue selectedAssembling;
        [ObservableProperty] private string selectedChooseAssembling;
        [ObservableProperty] private ObservableCollection<Device> deviceList = new();
        #endregion

        #region [ Properties - Communication Settings ]
        [ObservableProperty] private string selectedPort;
        [ObservableProperty] private int selectedBaudrate;
        [ObservableProperty]
        private ObservableCollection<string> lstPort = new();

        [ObservableProperty] private ObservableCollection<int> lstBaudrate = new();
        [ObservableProperty]
        private List<KeyValue> lstAssembling = new();

        #endregion
        #region [ Methods - Get Default setting ]

        private void GetDefaultSetting()
        {

            lstAssembling = new()
            {
                new KeyValue { key = "A", value = "Thành Hình A" },
                new KeyValue { key = "B", value = "Thành Hình B" },
                new KeyValue { key = "C", value = "Thành Hình C" },
                new KeyValue { key = "D", value = "Thành Hình D" }
            };

            lstBaudrate = new() { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };

            lstPort = new() { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM10" };
        }
        #endregion
        #region [ Methods - Language ]
        public void UpdateTexts()
        {
            AddDeviceCommandText = _languageService.GetString("Add a new device");
            EditDeviceCommandText = _languageService.GetString("Edit device");
            DeleteDeviceCommandText = _languageService.GetString("Delete device");
            NameDeviceCommandText = _languageService.GetString("Name device");
            AddressDeviceCommandText = _languageService.GetString("Address device");
            BaudrateDeviceCommandText = _languageService.GetString("Baudrate");
            PortDeviceCommandText = _languageService.GetString("Port");
            AssemblingCommandText = _languageService.GetString("Assembling");


        }
        #endregion

        #region [ Language Texts ]
        [ObservableProperty] private string addDeviceCommandText;
        [ObservableProperty] private string editDeviceCommandText;
        [ObservableProperty] private string deleteDeviceCommandText;
        [ObservableProperty] private string addressDeviceCommandText;
        [ObservableProperty] private string baudrateDeviceCommandText;
        [ObservableProperty] private string nameDeviceCommandText;
        [ObservableProperty] private string portDeviceCommandText;
        [ObservableProperty] private string assemblingCommandText;
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



        #endregion

        #region [ Methods - Selected Change ]
        partial void OnSelectedDeviceChanged(Device value)
        {
            if (value == null)
            {
                NameDevice = string.Empty;
                AddressDevice = 0;
                SelectedPort = null;
                SelectedBaudrate = 0;
                SelectedAssembling = null;
                SelectedChooseAssembling = null;
                IsEnableBtnEditDevice = false;
                return;
            }

            // Gán dữ liệu từ dòng đang chọn sang các input
            NameDevice = value.name;
            AddressDevice = value.address;
            SelectedPort = value.port;
            SelectedBaudrate = value.baudrate;

            // Nếu bạn có logic đặc biệt cho Thành hình & Type
            SelectedAssembling = LstAssembling.FirstOrDefault(x => x.key == value.assembling);

            // Cho phép nút Edit và Delete
            IsEnableBtnEditDevice = true;
            AddDeviceCommand.NotifyCanExecuteChanged();
            EditDeviceCommand.NotifyCanExecuteChanged();
            DeleteDeviceCommand.NotifyCanExecuteChanged();

        }


        #endregion



        #region [ Command Logic - Add Device (Sử dụng [RelayCommand]) ]
        [RelayCommand(CanExecute = nameof(CanExecuteAddDevice))]
        private async Task AddDevice()
        {
            try
            {
                if (!ValidateDeviceInput()) return;

                if (_context.devices.Where(x => x.typeid == 7)
                    .Any(x => x.name == NameDevice || x.address == AddressDevice))
                {
                    MessageBox.Show("Device already exists!");
                    return;
                }

                var newDevice = new Device
                {
                    name = NameDevice,
                    port = SelectedPort,
                    baudrate = SelectedBaudrate,
                    address = AddressDevice,
                    assembling = SelectedAssembling?.key,
                    typeid = 7,
                    activeid = 1,
                    ifshow = 1
                };

                await _service.InsertToDevice(newDevice);
                MessageBox.Show("Device added successfully!");
                LoadDeviceList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Add Device error: " + ex.Message);
            }
        }

        private bool CanExecuteAddDevice() => true; // ✅ luôn cho phép bấm
        #endregion


        #region [ Command Logic - Edit Device (Sử dụng [RelayCommand]) ]
        [RelayCommand(CanExecute = nameof(CanExecuteEditDevice))]
        private async Task EditDevice()
        {
            try
            {
                if (SelectedDevice == null)
                {
                    MessageBox.Show("No Device selected.");
                    return;
                }

                if (!ValidateDeviceInput()) return;

                var find = await _context.devices.FirstOrDefaultAsync(x => x.devid == SelectedDevice.devid);
                if (find == null)
                {
                    MessageBox.Show("Device not found.");
                    return;
                }

                find.address = AddressDevice;
                find.port = SelectedPort;
                find.baudrate = SelectedBaudrate;
                find.name = NameDevice;
                find.assembling = SelectedAssembling?.key;

                await _service.EditToDevice(find);
                MessageBox.Show("Edit successfully!");
                LoadDeviceList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Edit error: " + ex.Message);
            }
        }

        private bool CanExecuteEditDevice() => SelectedDevice != null; // ✅ chỉ bật khi chọn thiết bị
        #endregion


        #region [ Command Logic - Delete Device (Sử dụng [RelayCommand]) ]
        [RelayCommand(CanExecute = nameof(CanExecuteDeleteDevice))]
        private async Task DeleteDevice()
        {
            try
            {
                if (SelectedDevice == null)
                {
                    MessageBox.Show("No Device selected.");
                    return;
                }

                var device = await _context.devices.FirstOrDefaultAsync(x => x.devid == SelectedDevice.devid);
                if (device == null)
                {
                    MessageBox.Show("Device not found.");
                    return;
                }

                await _service.DeleteToDevice(device);
                MessageBox.Show("Delete successfully!");
                LoadDeviceList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete error: " + ex.Message);
            }
        }

        private bool CanExecuteDeleteDevice() => SelectedDevice != null; // ✅ chỉ bật khi chọn thiết bị
        #endregion


        #region [ Helper / Validation ]
        // Giữ nguyên hàm ValidateDeviceInput
        private bool ValidateDeviceInput()
        {
            if (string.IsNullOrWhiteSpace(NameDevice))
            {
                ErrorMessage = "NameDevice is required.";
                return false;
            }

            if (!Regex.IsMatch(NameDevice, @"^[a-zA-Z0-9 ]+$"))
            {
                ErrorMessage = "NameDevice cannot contain special characters.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(AddressDevice.ToString()) ||
                !int.TryParse(AddressDevice.ToString(), out int addr) || addr < 1 || addr > 50)
            {
                ErrorMessage = "AddressDevice must be a number between 1 and 50.";
                return false;
            }

            if (string.IsNullOrEmpty(SelectedPort) ||
                SelectedBaudrate == 0 ||
                SelectedAssembling.key == null)
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
