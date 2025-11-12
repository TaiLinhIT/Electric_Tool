using System.Windows.Controls;
using Electric_Meter.MVVM.ViewModels;
using Electric_Meter.Services;
using System.ComponentModel;

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
                e.PropertyName == nameof(_vm.AssemblingCommandText))
            {
                UpdateGridHeaders();
            }
        }

        private void UpdateGridHeaders()
        {
            if (DeviceGrid.Columns.Count >= 5)
            {
                DeviceGrid.Columns[0].Header = _vm.AddressDeviceCommandText;
                DeviceGrid.Columns[1].Header = _vm.NameDeviceCommandText;
                DeviceGrid.Columns[2].Header = _vm.BaudrateDeviceCommandText;
                DeviceGrid.Columns[3].Header = _vm.PortDeviceCommandText;
                DeviceGrid.Columns[4].Header = _vm.AssemblingCommandText;
            }
        }
    }
}
