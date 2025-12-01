using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // Quan trọng: Cung cấp [RelayCommand]

using Electric_Meter.Configs;
using Electric_Meter.Dto.DeviceDto;
using Electric_Meter.Models;
using Electric_Meter.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly PowerTempWatchContext _context;
        #endregion

        #region [ Events ]
        public event Action OnDeviceLoadDefault;
        public event Action<Button, Button> NewButtonCreated;
        #endregion

        #region [ Constructor ]
        public SettingViewModel(LanguageService languageService, Service service, ToolViewModel toolViewModel, AppSetting appSetting, IServiceScopeFactory serviceScope, PowerTempWatchContext context)
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
        [ObservableProperty] private DeviceDto selectedDevice;//  phải có cái này mới có onselectedchange
        [ObservableProperty] private ControlcodeVM selectedControlCode;

        #endregion

        #region [ Properties - Device Configuration ]
        [ObservableProperty] private string nameDevice = string.Empty;
        [ObservableProperty] private int devid;
        [ObservableProperty] private string selectedActive;
        [ObservableProperty] private string selectedSensorType;
        [ObservableProperty] private ObservableCollection<DeviceDto> deviceList = new();
        [ObservableProperty] private ObservableCollection<ControlcodeVM> controlCodeList = new();
        #endregion
        #region [ Properties - Controlcode Configuration ]
        [ObservableProperty] private int codeId;
        [ObservableProperty] private int devId;
        [ObservableProperty] private string code;
        [ObservableProperty] private string active;
        [ObservableProperty] private string codeType;
        [ObservableProperty] private string name;
        [ObservableProperty] private double factor;
        [ObservableProperty] private string type;
        [ObservableProperty] private decimal? high;
        [ObservableProperty] private decimal? low;
        [ObservableProperty] private int? ifShow;
        [ObservableProperty] private int? ifCal;
        [ObservableProperty] private int activeid;
        public string ActiveText => activeid == 1 ? ActiveCommandText : InActiveCommandText;

        #endregion
        #region [ Properties - Communication Settings ]
        [ObservableProperty] private ObservableCollection<string> lstPort = new();

        [ObservableProperty] private ObservableCollection<string> lstActive = new();
        [ObservableProperty] private ObservableCollection<string> lstSensorType = new();
        [ObservableProperty]
        private List<KeyValue> lstAssembling = new();

        #endregion
        #region [ Methods - Get Default setting ]

        private void GetDefaultSetting()
        {

            //SetupAssemblingList();

            LstActive = new ObservableCollection<string>(_service.GetActiveTypes().Select(x => x.name));
            LstSensorType = new ObservableCollection<string>(_service.GetSensorTypes().Select(x =>x.name));
        }
        //private void SetupAssemblingList()
        //{
        //    // Bao bọc toàn bộ logic trong Dispatcher.Invoke() để đảm bảo an toàn luồng
        //    Application.Current.Dispatcher.Invoke(() =>
        //    {
        //        // Cập nhật lại danh sách LstAssembling
        //        LstAssembling = new()
        //        {
        //            new KeyValue { key = "A", value = $"{AssemblingText} A" },
        //            new KeyValue { key = "B", value = $"{AssemblingText} B" },
        //            new KeyValue { key = "C", value = $"{AssemblingText} C" },
        //            new KeyValue { key = "D", value = $"{AssemblingText} D" }
        //        };

        //        // Đảm bảo giữ lại KeyValue đã chọn
        //        if (SelectedAssembling != null)
        //        {
        //            var newSelected = LstAssembling.FirstOrDefault(x => x.key == SelectedAssembling.key);

        //            if (newSelected != null)
        //            {
        //                // Gán trực tiếp vì đây là ObservableProperty (sẽ kích hoạt OnSelectedAssemblingChanged)
        //                SelectedAssembling = newSelected;
        //            }
        //        }
        //        // Nếu SelectedAssembling là null (lần chạy đầu), gán lại phần tử đầu tiên
        //        else
        //        {
        //            SelectedAssembling = LstAssembling.FirstOrDefault();
        //        }
        //    });

        //}
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
            CodeTypeIdCommandText = _languageService.GetString("CodeTypeId");
            NameCommandText = _languageService.GetString("Name");
            FactorCommandText = _languageService.GetString("Factor");
            TypeIdCommandText = _languageService.GetString("TypeId");
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
        [ObservableProperty] private string codeTypeIdCommandText;
        [ObservableProperty] private string nameCommandText;
        [ObservableProperty] private string factorCommandText;
        [ObservableProperty] private string typeIdCommandText;
        [ObservableProperty] private string highCommandText;
        [ObservableProperty] private string lowCommandText;
        [ObservableProperty] private string ifShowCommandText;
        [ObservableProperty] private string ifCalCommandText;
        [ObservableProperty] private string nameCodeTypeCommandText;
        [ObservableProperty] private string nameTypeCommandText;
        #endregion

        #region [ Methods - Load & Initialization ]
        // Giữ nguyên các hàm Load
        private async Task LoadDeviceList()
        {
            try
            {
                var devices = await _service.GetListDevice();
                DeviceList = new(devices);

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
            LoadControlCodeList(value.devid);
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

        partial void OnSelectedControlCodeChanged(ControlcodeVM value)
        {
            if (value == null)
            {
                CodeId = 0;
                DevId = 0;
                Code = string.Empty;
                Active = string.Empty;
                CodeType = string.Empty;
                Name = string.Empty;
                Factor = 0;
                Type = string.Empty;
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
            //Active = value.activeid;
            CodeType = value.codetype;
            Name = value.name;
            Factor = value.factor;
            Type = value.type;
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
                

                var newDevice = new CreateDeviceDto
                {
                    devid = Devid,
                    name = NameDevice,
                    typeid = _service.GetSensorTypes().FirstOrDefault(x => x.name == SelectedSensorType) ?.typeid ?? 0,
                    activeid = _service.GetActiveTypes().FirstOrDefault(x => x.name == SelectedActive)?.activeid ?? 0,
                    ifshow = 1 // Mặc định hiển thị
                };

                await _service.CreateDevice(newDevice);
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
                find.name = NameDevice;
                find.typeid = _service.GetSensorTypes().FirstOrDefault(x => x.name == SelectedSensorType)?.typeid ?? find.typeid;
                find.activeid = _service.GetActiveTypes().FirstOrDefault(x => x.name == SelectedActive)?.activeid ?? find.activeid;
                find.ifshow = 1; // Giữ nguyên hiển thị
                await _service.UpdateDevice(find);
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
                using var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var newControlCode = new Controlcode
                {
                    code = Code,
                    activeid = _context.activeTypes.Where(x => x.name == Active).Select(x => x.activeid).FirstOrDefault(),
                    codetypeid = _context.codetypes.Where(x => x.name == CodeType).Select(x => x.codetypeid).FirstOrDefault(),
                    name = Name,
                    factor = Factor,
                    typeid = _context.sensorTypes.Where(x => x.name == Type).Select(x => x.typeid).FirstOrDefault(),
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
                //find.activeid = ActiveId;
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

           

            ErrorMessage = string.Empty;
            return true;
        }
        #endregion
    }
}
