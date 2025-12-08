using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // Quan trọng: Cung cấp [RelayCommand]

using Electric_Meter.Configs;
using Electric_Meter.Dto.ControlcodeDto;
using Electric_Meter.Dto.DeviceDto;
using Electric_Meter.Interfaces;
using Electric_Meter.Models;
using Electric_Meter.Services;

using Microsoft.Extensions.DependencyInjection;
// Lưu ý: Đảm bảo rằng lớp RelayCommand cũ trong Electric_Meter.Core đã được loại bỏ 
// hoặc bạn đã xóa using Electric_Meter.Core; để tránh xung đột.

namespace Electric_Meter.MVVM.ViewModels
{
    public partial class SettingViewModel : ObservableObject
    {
        #region [ Fields - Private Dependencies ]
        private readonly LanguageService _languageService;
        private readonly IService _service;
        private readonly ToolViewModel _toolViewModel;
        private readonly AppSetting _appSetting;
        private readonly IServiceScopeFactory _scopeFactory;
        #endregion

        #region [ Events ]
        public event Action OnDeviceLoadDefault;
        public event Action<Button, Button> NewButtonCreated;
        #endregion

        #region [ Constructor ]
        public SettingViewModel(LanguageService languageService, IService service, ToolViewModel toolViewModel, AppSetting appSetting, IServiceScopeFactory serviceScope)
        {
            _languageService = languageService;
            _languageService.LanguageChanged += UpdateTexts;

            UpdateTexts();
            _service = service;
            _toolViewModel = toolViewModel;
            _appSetting = appSetting;

            // Khởi tạo trạng thái ban đầu
            // Các thuộc tính [ObservableProperty] có giá trị mặc định là true/false
            IsEnabledBtnConnect = true;
            IsEnabledBtnAddDevice = true;
            IsEnableBtnEditDevice = false;
            IsEnabledBtnDeleteDevice = true; // Mặc định là true, sẽ được kiểm soát bởi CanDelete

            // Load dữ liệu ban đầu
            //LoadAssemblings();
            LoadDeviceListAsync();
            _ = LoadControlCodeList();
            GetDefaultSettingAsync();

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
        [ObservableProperty] private DeviceDto selectedDevice;//  phải có cái này mới có onselectedchange
        [ObservableProperty] private ControlcodeDto selectedControlCode;

        #endregion

        #region [ Properties - Device Configuration ]
        [ObservableProperty] private string nameDevice = string.Empty;
        [ObservableProperty] private int devid;
        [ObservableProperty] private string selectedActive;
        [ObservableProperty] private string selectedSensorType;
        [ObservableProperty] private ObservableCollection<DeviceDto> deviceList = new();
        [ObservableProperty] private ObservableCollection<ControlcodeDto> controlCodeList = new();
        #endregion
        #region [ Properties - Controlcode Configuration ]
        [ObservableProperty] private int codeId;
        [ObservableProperty] private ObservableCollection<string> lstActiveControlcode = new();
        [ObservableProperty] private ObservableCollection<string> lstDeviceControlcode = new();
        [ObservableProperty] private string selectedDeviceName;
        [ObservableProperty] private string selectedActiveControlcodeName;
        [ObservableProperty] private string code;
        [ObservableProperty] private string activeControlcode;
        [ObservableProperty] private string codeType;
        [ObservableProperty] private string nameControlcode;
        [ObservableProperty] private double factor;
        [ObservableProperty] private string sensorType;
        [ObservableProperty] private decimal? high;
        [ObservableProperty] private decimal? low;
        [ObservableProperty] private int activeid;
        public string ActiveText => activeid == 1 ? ActiveCommandText : InActiveCommandText;

        #endregion
        #region [ Properties - Code type configuration ]
        [ObservableProperty] private ObservableCollection<string> lstCodeType = new();
        [ObservableProperty] private string selectedCodeType;
        #endregion
        #region [ Properties - Communication Settings ]
        [ObservableProperty] private ObservableCollection<string> lstPort = new();

        [ObservableProperty] private ObservableCollection<string> lstActive = new();
        [ObservableProperty] private ObservableCollection<string> lstSensorType = new();
        [ObservableProperty] private ObservableCollection<string> lstSensorTypeControlCode = new();
        [ObservableProperty] private string selectedSensorTypeControlCode;

        [ObservableProperty] private List<KeyValue> lstAssembling = new();

        #endregion

        #region [ Methods - Get Default setting ]

        private async Task GetDefaultSettingAsync()
        {

            //SetupAssemblingList();

            LstActive = new(
                (await _service.GetActiveTypesAsync()).Select(x => x.name)
                );
            LstSensorType = new(
                (await _service.GetSensorTypesAsync()).Select(x => x.Name)
                );
            LstSensorTypeControlCode = new(
                (await _service.GetSensorTypesAsync()).Select(x => x.Name)
                );
            LstActiveControlcode = new(
                (await _service.GetActiveTypesAsync()).Select(x => x.name)
                );
            LstDeviceControlcode = new((await _service.GetListDeviceAsync()).Select(x => x.name));
            LstCodeType = new((await _service.GetCodeTypeAsync()).Select(x => x.NameCodeType));

        }

        #endregion
        #region [ Methods - Language ]
        public void UpdateTexts()
        {
            SenSorTypeCommandText = _languageService.GetString("Sensor type");
            AddDeviceCommandText = _languageService.GetString("Add a new device");
            EditDeviceCommandText = _languageService.GetString("Edit device");
            DeleteDeviceCommandText = _languageService.GetString("Delete device");
            AddControlCodeCommandText = _languageService.GetString("Add a new controlcode");
            EditControlCodeCommandText = _languageService.GetString("Edit controlcode");
            DeleteControlCodeCommandText = _languageService.GetString("Delete controlcode");
            NameDeviceCommandText = _languageService.GetString("Name device");
            AddressDeviceCommandText = _languageService.GetString("Address device");
            DevidCommandText = _languageService.GetString("Device id");
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
            ActiveCommandText = _languageService.GetString("Active");
            InActiveCommandText = _languageService.GetString("InActive");
            CodeTypeCommandText = _languageService.GetString("CodeType");
            NameCommandText = _languageService.GetString("Name");
            FactorCommandText = _languageService.GetString("Factor");
            //SensorTypeCommandText = _languageService.GetString("Sensor type");
            HighCommandText = _languageService.GetString("High");
            LowCommandText = _languageService.GetString("Low");
            IfShowCommandText = _languageService.GetString("IfShow");
            IfCalCommandText = _languageService.GetString("IfCal");
            NameCodeTypeCommandText = _languageService.GetString("Name code type");
            NameTypeCommandText = _languageService.GetString("Name type");

            //Application.Current.Dispatcher.Invoke(() =>
            //{
            //    SetupAssemblingList();
            //});


        }
        #endregion

        #region [ Language Texts ]
        [ObservableProperty] private string senSorTypeCommandText;
        [ObservableProperty] private string addDeviceCommandText;
        [ObservableProperty] private string editDeviceCommandText;
        [ObservableProperty] private string deleteDeviceCommandText;
        [ObservableProperty] private string addressDeviceCommandText;
        [ObservableProperty] private string devidCommandText;
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
        [ObservableProperty] private string activeCommandText;
        [ObservableProperty] private string inActiveCommandText;
        [ObservableProperty] private string codeTypeCommandText;
        [ObservableProperty] private string nameCommandText;
        [ObservableProperty] private string factorCommandText;
        [ObservableProperty] private string sensorTypeCommandText;
        [ObservableProperty] private string highCommandText;
        [ObservableProperty] private string lowCommandText;
        [ObservableProperty] private string ifShowCommandText;
        [ObservableProperty] private string ifCalCommandText;
        [ObservableProperty] private string nameCodeTypeCommandText;
        [ObservableProperty] private string nameTypeCommandText;
        #endregion

        #region [ Methods - Load & Initialization ]
        // Giữ nguyên các hàm Load
        private async Task LoadDeviceListAsync()
        {
            try
            {
                var devices = await _service.GetListDeviceAsync();
                DeviceList = new(devices);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading devices: " + ex.Message);
            }
        }
        private async Task LoadControlCodeListByDevid(int devid)
        {
            try
            {
                if (devid == null)
                {
                    var lstcontrolcode = await _service.GetListControlcodeAsync();
                    ControlCodeList = new(lstcontrolcode);

                }
                else
                {
                    var lstcontrolcode = await _service.GetControlcodeByDevidAsync(devid);
                    ControlCodeList = new(lstcontrolcode);
                }


            }
            catch (Exception ex)
            {

                MessageBox.Show("Error loading control code: " + ex.Message);
            }
        }
        private async Task LoadControlCodeList()
        {
            try
            {

                var lstcontrolcode = await _service.GetListControlcodeAsync();
                ControlCodeList = new(lstcontrolcode);


            }
            catch (Exception ex)
            {

                MessageBox.Show("Error loading control code: " + ex.Message);
            }
        }



        #endregion

        #region [ Methods - Selected Change ]
        partial void OnSelectedDeviceChanged(DeviceDto value)
        {
            if (value == null)
            {
                NameDevice = string.Empty;
                SelectedActive = string.Empty;
                SelectedSensorType = string.Empty;
                IsEnableBtnEditDevice = false;
                return;
            }
            // Gán dữ liệu từ dòng đang chọn sang các input
            Devid = value.devid;
            NameDevice = value.name;
            SelectedActive = value.active;
            SelectedSensorType = value.type;


            // Cho phép nút Edit và Delete
            IsEnableBtnEditDevice = true;
            AddDeviceCommand.NotifyCanExecuteChanged();
            EditDeviceCommand.NotifyCanExecuteChanged();
            DeleteDeviceCommand.NotifyCanExecuteChanged();

        }
        partial void OnDevidChanged(int devid)
        {
            _ = LoadControlCodeListByDevid(devid);
        }


        partial void OnSelectedControlCodeChanged(ControlcodeDto value)
        {
            if (value == null)
            {
                CodeId = 0;
                Code = string.Empty;
                ActiveControlcode = string.Empty;
                CodeType = string.Empty;
                NameControlcode = string.Empty;
                Factor = 0;
                SensorType = string.Empty;
                High = 0;
                Low = 0;
                return;
            }
            // Gán dữ liệu từ dòng đang chọn sang các input
            CodeId = value.CodeId;
            Code = value.Code;
            SelectedActiveControlcodeName = value.Active;
            SelectedCodeType = value.CodeType;
            SelectedDeviceName = value.DeviceName;
            NameControlcode = value.NameControlcode;
            Factor = value.Factor;
            SelectedSensorTypeControlCode = value.SensorType;
            High = value.High;
            Low = value.Low;
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


                var newDevice = new CreateDeviceDto
                {
                    devid = Devid,
                    name = NameDevice,
                    type = SelectedSensorType,
                    active = SelectedActive,
                    ifshow = 1 // Mặc định hiển thị
                };

                await _service.CreateDeviceAsync(newDevice);
                LoadDeviceListAsync();
                GetDefaultSettingAsync();
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

                var device = new EditDeviceDto()
                {
                    devid = SelectedDevice.devid,
                    name = NameDevice,
                    type = selectedSensorType,
                    active = selectedActive,
                    ifshow = 1 // Giữ nguyên hiển thị
                };
                await _service.UpdateDeviceAsync(device);
                LoadDeviceListAsync();
                GetDefaultSettingAsync();
                MessageBox.Show("Edit successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Edit error: " + ex.Message);
            }
        }

        private bool CanExecuteEditDevice() => SelectedDevice != null; //Chỉ bật khi chọn thiết bị
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

                int devid = Devid;
                if (devid == null)
                {
                    MessageBox.Show("Device not found.");
                    return;
                }

                await _service.DeleteDeviceAsync(devid);
                MessageBox.Show("Delete successfully!");
                LoadDeviceListAsync();
                GetDefaultSettingAsync();
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
                bool check = false;
                if (check)
                {
                    MessageBox.Show("Control code exist!");
                    return;
                }
                var newControlCode = new CreateControlcodeDto()
                {
                    CodeId = CodeId,
                    DeviceName = SelectedDeviceName,
                    CodeType = SelectedCodeType,
                    Active = SelectedActiveControlcodeName,
                    Code = Code,
                    NameControlcode = NameControlcode,
                    Factor = Factor,
                    SensorType = SelectedSensorTypeControlCode,
                    High = High,
                    Low = Low

                };
                await _service.CreateControlcodeAsync(newControlCode);
                _ = LoadControlCodeList();
                MessageBox.Show("Control Code added successfully!");
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
                //var find = await _context.controlcodes.FirstOrDefaultAsync(x => x.codeid == SelectedControlCode.CodeId);
                var find = 9;
                //var find = await _service.
                if (find == null)
                {
                    MessageBox.Show("Control Code not found.");
                    return;
                }
                var dto = new EditControlcodeDto()
                {
                    CodeId = CodeId,
                    DeviceName = SelectedDeviceName,
                    CodeType = SelectedCodeType,
                    Active = SelectedActiveControlcodeName,
                    Code = Code,
                    NameControlcode = NameControlcode,
                    Factor = Factor,
                    SensorType = SelectedSensorTypeControlCode,
                    High = High,
                    Low = Low
                };
                await _service.UpdateControlcodeAsync(dto);
                _ = LoadControlCodeList();
                MessageBox.Show("Edit successfully!");
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
            try
            {
                if (CodeId == null)
                {
                    MessageBox.Show("No Device selected.");
                    return;
                }


                await _service.DeleteControlcodeAsync(CodeId);
                _ = LoadControlCodeList();
                MessageBox.Show("Delete successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete error: " + ex.Message);
            }
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



            ErrorMessage = string.Empty;
            return true;
        }
        #endregion
    }
}
