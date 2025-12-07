using System.ComponentModel;
using System.Windows.Controls;

using Electric_Meter.MVVM.ViewModels;
using Electric_Meter.Services;

namespace Electric_Meter.MVVM.Views
{
    /// <summary>
    /// Interaction logic for CommandManagerView.xaml
    /// </summary>
    public partial class CommandManagerView : UserControl
    {
        private readonly CommandManagerViewModel _vm;
        public CommandManagerView(CommandManagerViewModel vm)
        {
            InitializeComponent();
            DataContext = _vm = vm;
            Loaded += (s, e) =>
            {
                UpdateGridHeaders();

                // Khi có thay đổi text trong ViewModel (sau khi đổi ngôn ngữ)
                _vm.PropertyChanged += Vm_PropertyChanged;

                // Nếu có LanguageService, lắng nghe sự kiện đổi ngôn ngữ
                var langField = typeof(CommandManagerViewModel)
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
            if (e.PropertyName == nameof(_vm.CodeTypeText) ||
                e.PropertyName == nameof(_vm.NameText)
                )

            {
                UpdateGridHeaders();
            }
        }

        private void UpdateGridHeaders()
        {
            if (CodeTypeGrid.Columns.Count >= 3)
            {
                CodeTypeGrid.Columns[1].Header = _vm.CodeTypeText;
                CodeTypeGrid.Columns[2].Header = _vm.NameText;
            }
        }
    }
}
