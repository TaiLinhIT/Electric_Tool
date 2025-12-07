using System.Windows;

using Electric_Meter.MVVM.ViewModels;

namespace Electric_Meter.MVVM.Views
{
    /// <summary>
    /// Interaction logic for DeviceManagerView.xaml
    /// </summary>
    public partial class DeviceManagerView : Window
    {
        private readonly DeviceManagerViewModel _vm;
        public DeviceManagerView(DeviceManagerViewModel vm)
        {
            InitializeComponent();
            DataContext = _vm = vm;
        }
    }
}
