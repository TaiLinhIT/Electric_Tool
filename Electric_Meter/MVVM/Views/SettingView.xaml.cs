using System.ComponentModel;
using System.Windows.Controls;

using Electric_Meter.MVVM.ViewModels;
using Electric_Meter.Services;

namespace Electric_Meter.MVVM.Views
{
    public partial class SettingView : UserControl
    {
        private SettingViewModel _vm;

        public SettingView(SettingViewModel settingViewModel)
        {
            InitializeComponent();
            DataContext = _vm = settingViewModel;

            Loaded += (s, e) =>
            {
                UpdateGridHeaders();

                // Khi có thay đổi text trong ViewModel (sau khi đổi ngôn ngữ)
                _vm.PropertyChanged += Vm_PropertyChanged;

                // Nếu có LanguageService, lắng nghe sự kiện đổi ngôn ngữ
                var langField = typeof(SettingViewModel)
                    .GetField("_languageService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (langField?.GetValue(_vm) is LanguageService langService)
                {
                    langService.LanguageChanged += () =>
                    {
                        // Khi ngôn ngữ đổi, cập nhật text trong ViewModel
                        _vm.UpdateTexts();

                        // Cập nhật lại header trong DataGrid
                        UpdateGridHeaders();
                    };
                }
            };
        }

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_vm.AddressDeviceCommandText) ||
                e.PropertyName == nameof(_vm.NameDeviceCommandText) ||
                e.PropertyName == nameof(_vm.BaudrateDeviceCommandText) ||
                e.PropertyName == nameof(_vm.PortDeviceCommandText) ||
                e.PropertyName == nameof(_vm.AssemblingCommandText) ||
                e.PropertyName == nameof(_vm.CodeIdCommandText) ||
                e.PropertyName == nameof(_vm.DevIdCommandText) ||
                e.PropertyName == nameof(_vm.CodeCommandText) ||
                e.PropertyName == nameof(_vm.ActiveCommandText) ||
                e.PropertyName == nameof(_vm.CodeTypeCommandText) ||
                e.PropertyName == nameof(_vm.NameCommandText) ||
                e.PropertyName == nameof(_vm.FactorCommandText) ||
                e.PropertyName == nameof(_vm.SensorTypeCommandText) ||
                e.PropertyName == nameof(_vm.HighCommandText) ||
                e.PropertyName == nameof(_vm.LowCommandText) ||
                e.PropertyName == nameof(_vm.NameCodeTypeCommandText) ||
                e.PropertyName == nameof(_vm.NameTypeCommandText)
                )

            {
                UpdateGridHeaders();
            }
        }

        private void UpdateGridHeaders()
        {
            if (DeviceGrid.Columns.Count >= 5)
            {
                DeviceGrid.Columns[0].Header = _vm.DevidCommandText;
                DeviceGrid.Columns[1].Header = _vm.NameDeviceCommandText;
                DeviceGrid.Columns[2].Header = _vm.ActiveCommandText;
                DeviceGrid.Columns[3].Header = _vm.SenSorTypeCommandText;
                DeviceGrid.Columns[4].Header = _vm.IfShowCommandText;
            }
            if (ControlcodeGrid.Columns.Count >= 10)
            {
                ControlcodeGrid.Columns[0].Header = _vm.CodeIdCommandText;
                ControlcodeGrid.Columns[1].Header = _vm.NameDeviceCommandText;
                ControlcodeGrid.Columns[2].Header = _vm.CodeCommandText;
                ControlcodeGrid.Columns[3].Header = _vm.ActiveCommandText;
                ControlcodeGrid.Columns[4].Header = _vm.SenSorTypeCommandText;
                ControlcodeGrid.Columns[5].Header = _vm.NameCodeTypeCommandText;
                ControlcodeGrid.Columns[6].Header = _vm.NameCommandText;
                ControlcodeGrid.Columns[7].Header = _vm.FactorCommandText;
                ControlcodeGrid.Columns[8].Header = _vm.HighCommandText;
                ControlcodeGrid.Columns[9].Header = _vm.LowCommandText;

            }
        }
    }
}
