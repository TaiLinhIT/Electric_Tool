using System.Windows.Controls;

using Electric_Meter.MVVM.ViewModels;

namespace Electric_Meter.MVVM.Views
{
    /// <summary>
    /// Interaction logic for SystemOfParameterView.xaml
    /// </summary>
    public partial class SystemOfParameterView : UserControl
    {
        private readonly SystemOfParameterViewModel _vm;
        public SystemOfParameterView(SystemOfParameterViewModel vm)
        {
            InitializeComponent();
            DataContext = _vm = vm;
        }


    }
}
