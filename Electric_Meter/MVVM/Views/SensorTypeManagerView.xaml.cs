using System.ComponentModel;
using System.Windows.Controls;

using Electric_Meter.MVVM.ViewModels;
using Electric_Meter.Services;

namespace Electric_Meter.MVVM.Views
{
    /// <summary>
    /// Interaction logic for SensorTypeManagerView.xaml
    /// </summary>
    public partial class SensorTypeManagerView : UserControl
    {
        private readonly SensorTypeManagerViewModel _vm;
        public SensorTypeManagerView(SensorTypeManagerViewModel vm)
        {
            InitializeComponent();
            DataContext = _vm = vm;
            Loaded += (s, e) =>
            {
                UpdateGridHeaders();

                // Khi có thay đổi text trong ViewModel (sau khi đổi ngôn ngữ)
                _vm.PropertyChanged += Vm_PropertyChanged;

                // Nếu có LanguageService, lắng nghe sự kiện đổi ngôn ngữ
                var langField = typeof(SensorTypeManagerViewModel)
                    .GetField("_languageService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (langField?.GetValue(_vm) is LanguageService langService)
                {
                    langService.LanguageChanged += () =>
                    {
                        // Khi ngôn ngữ đổi, cập nhật text trong ViewModel
                        _vm.UpdateTexts();

                        // Cập nhật lại header trong DataGrid2
                        UpdateGridHeaders();
                    };
                }
            };
        }
        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_vm.TypeIdText) ||
                e.PropertyName == nameof(_vm.NameText) 
                )

            {
                UpdateGridHeaders();
            }
        }

        private void UpdateGridHeaders()
        {
            if (SensorTypeGrid.Columns.Count >= 2)
            {
                SensorTypeGrid.Columns[0].Header = _vm.TypeIdText;
                SensorTypeGrid.Columns[1].Header = _vm.NameText;
            }
        }
    }
}
