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
        [ObservableProperty] private bool isEnabledBtnAddControlCode;
        [ObservableProperty] private bool isEnabledBtnEditControlCode;
        [ObservableProperty] private bool isEnabledBtnDeleteControlCode;
        [ObservableProperty] private string errorMessage;
        [ObservableProperty] private Device selectedDevice;//  phải có cái này mới có onselectedchange
        [ObservableProperty] private Controlcode selectedControlCode;

        #endregion

        #region [ Properties - Device Configuration ]
        [ObservableProperty] private string nameDevice = string.Empty;
        [ObservableProperty] private int addressDevice;
        [ObservableProperty] private KeyValue selectedAssembling;
        [ObservableProperty] private string selectedChooseAssembling;
        [ObservableProperty] private ObservableCollection<Device> deviceList = new();
        [ObservableProperty] private ObservableCollection<Controlcode> controlCodeList = new();
        #endregion
        #region [ Properties - Controlcode Configuration ]
        [ObservableProperty] private int codeId;
        [ObservableProperty] private int devId;
        [ObservableProperty] private string code;
        [ObservableProperty] private int activeId;
        [ObservableProperty] private string codeTypeId;
        [ObservableProperty] private string name;
        [ObservableProperty] private double factor;
        [ObservableProperty] private int? typeId;
        [ObservableProperty] private decimal? high;
        [ObservableProperty] private decimal? low;
        [ObservableProperty] private int? ifShow;
        [ObservableProperty] private int? ifCal;
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

            SetupAssemblingList();

            lstBaudrate = new() { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };

            lstPort = new() { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM10" };
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
        #endregion
        #region [ Methods - Language ]
        public void UpdateTexts()
        {
            AddDeviceCommandText = _languageService.GetString("Add a new device");
            EditDeviceCommandText = _languageService.GetString("Edit device");
            DeleteDeviceCommandText = _languageService.GetString("Delete device");
            AddControlCodeCommandText = _languageService.GetString("Add a new controlcode");
            EditControlCodeCommandText = _languageService.GetString("Edit controlcode");
            DeleteControlCodeCommandText = _languageService.GetString("Delete controlcode");
            NameDeviceCommandText = _languageService.GetString("Name device");
            AddressDeviceCommandText = _languageService.GetString("Address device");
            BaudrateDeviceCommandText = _languageService.GetString("Baudrate");
            PortDeviceCommandText = _languageService.GetString("Port");
            AssemblingCommandText = _languageService.GetString("Assembling");
            AssemblingText = _languageService.GetString("Assembling");
            DetailControlCodeText = _languageService.GetString("Detail controlcode");
            ListControlCodeText = _languageService.GetString("List controlcode");
            DetailDeviceText = _languageService.GetString("Detail device");
            ListDeviceText = _languageService.GetString("List device");
            AddControlCodeCommandText = _languageService.GetString("Add a new controlcode");
            EditControlCodeCommandText = _languageService.GetString("Edit controlcode");
            DeleteControlCodeCommandText = _languageService.GetString("Delete controlcode");
            CodeIdCommandText = _languageService.GetString("CodeId");
            DevIdCommandText = _languageService.GetString("DevId");
            CodeCommandText = _languageService.GetString("Code");
            ActiveIdCommandText = _languageService.GetString("ActiveId");
            CodeTypeIdCommandText = _languageService.GetString("CodeTypeId");
            NameCommandText = _languageService.GetString("Name");
            FactorCommandText = _languageService.GetString("Factor");
            TypeIdCommandText = _languageService.GetString("TypeId");
            HighCommandText = _languageService.GetString("High");
            LowCommandText = _languageService.GetString("Low");
            IfShowCommandText = _languageService.GetString("IfShow");
            IfCalCommandText = _languageService.GetString("IfCal");
            Application.Current.Dispatcher.Invoke(() =>
            {
                SetupAssemblingList();
            });


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
        [ObservableProperty] private string assemblingText;

        [ObservableProperty] private string detailControlCodeText;
        [ObservableProperty] private string listControlCodeText;
        [ObservableProperty] private string detailDeviceText;
        [ObservableProperty] private string listDeviceText;
        [ObservableProperty] private string addControlCodeCommandText;
        [ObservableProperty] private string editControlCodeCommandText;
        [ObservableProperty] private string deleteControlCodeCommandText;
        [ObservableProperty] private string codeIdCommandText;
        [ObservableProperty] private string devIdCommandText;
        [ObservableProperty] private string codeCommandText;
        [ObservableProperty] private string activeIdCommandText;
        [ObservableProperty] private string codeTypeIdCommandText;
        [ObservableProperty] private string nameCommandText;
        [ObservableProperty] private string factorCommandText;
        [ObservableProperty] private string typeIdCommandText;
        [ObservableProperty] private string highCommandText;
        [ObservableProperty] private string lowCommandText;
        [ObservableProperty] private string ifShowCommandText;
        [ObservableProperty] private string ifCalCommandText;
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
        private void LoadControlCodeList(int devid)
        {
            try
            {
                var controlcodes = _service.GetControlCodeListByDevid(devid);
                ControlCodeList = new(controlcodes);
            }
            catch (Exception ex)
            {

                MessageBox.Show("Error loading control code: " + ex.Message);
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
            LoadControlCodeList(value.devid);
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

        partial void OnSelectedControlCodeChanged(Controlcode value)
        {
            if (value == null)
            {
                CodeId = 0;
                DevId = 0;
                Code = string.Empty;
                ActiveId = 0;
                CodeTypeId = string.Empty;
                Name = string.Empty;
                Factor = 0;
                TypeId = 0;
                High = 0;
                Low = 0;
                IfShow = 0;
                IfCal = 0;
                return;
            }
            // Gán dữ liệu từ dòng đang chọn sang các input
            CodeId = value.codeid;
            DevId = value.devid;
            Code = value.code;
            ActiveId = value.activeid;
            CodeTypeId = value.codetypeid;
            Name = value.name;
            Factor = value.factor;
            TypeId = value.typeid;
            High = value.high;
            Low = value.low;
            IfShow = value.ifshow;
            IfCal = value.ifcal;
            AddControlCodeCommand.NotifyCanExecuteChanged();
            EditControlCodeCommand.NotifyCanExecuteChanged();
            DeleteControlCodeCommand.NotifyCanExecuteChanged();
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

        #region [ Command Logic - Add Control Code ]
        [RelayCommand(CanExecute = nameof(CanExecuteAddControlCode))]
        private async Task AddControlCode()
        {
            try
            {
                var newControlCode = new Controlcode
                {
                    code = Code,
                    activeid = ActiveId,
                    codetypeid = CodeTypeId,
                    name = Name,
                    factor = Factor,
                    typeid = TypeId,
                    high = High,
                    low = Low,
                    ifshow = IfShow,
                    ifcal = IfCal
                };
                await _service.InsertToControlcode(newControlCode);
                MessageBox.Show("Control Code added successfully!");
                LoadControlCodeList(SelectedDevice.devid);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);

            }
        }
        private bool CanExecuteAddControlCode() => true; // Chỉ bật khi có thiết bị được chọn
        #endregion
        #region [ Command Logic - Edit Control Code ]
        [RelayCommand(CanExecute = nameof(CanExecuteEditControlCode))]
        private async Task EditControlCode()
        {
            try
            {
                var find = await _context.controlcodes.FirstOrDefaultAsync(x => x.codeid == SelectedControlCode.codeid);
                if (find == null)
                {
                    MessageBox.Show("Control Code not found.");
                    return;
                }
                find.code = Code;
                find.activeid = ActiveId;
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        private bool CanExecuteEditControlCode() => SelectedControlCode != null; // Chỉ bật khi có Control Code được chọn
        #endregion
        #region [ Command Logic - Delete Control Code ]
        [RelayCommand(CanExecute = nameof(CanExecuteDeleteControlCode))]
        private async Task DeleteControlCode()
        {
            // Thêm logic xóa Control Code ở đây
        }
        private bool CanExecuteDeleteControlCode() => SelectedControlCode != null; // Chỉ bật khi có Control Code được chọn
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
                !int.TryParse(AddressDevice.ToString(), out int addr) || addr < 1)
            {
                ErrorMessage = "AddressDevice must be more than 1";
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
