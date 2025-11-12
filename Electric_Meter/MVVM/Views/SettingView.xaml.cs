using System.Windows.Controls;

using Electric_Meter.MVVM.ViewModels;

namespace Electric_Meter.MVVM.Views
{
    /// <summary>
    /// Interaction logic for SettingView.xaml
    /// </summary>
    public partial class SettingView : UserControl
    {
        public SettingView(SettingViewModel settingViewModel)
        {
            InitializeComponent();
            DataContext = settingViewModel;
        }
    }
}
