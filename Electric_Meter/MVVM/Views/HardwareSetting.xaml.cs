using System.Windows.Controls;

using Electric_Meter.MVVM.ViewModels;

namespace Electric_Meter.MVVM.Views
{
    /// <summary>
    /// Interaction logic for HardwareSetting.xaml
    /// </summary>
    public partial class HardwareSetting : UserControl
    {
        private readonly HardwareViewModel _vm;
        public HardwareSetting(HardwareViewModel vm)
        {
            InitializeComponent();
            DataContext = _vm = vm;
        }
    }
}
