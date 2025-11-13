using System.Windows.Controls;


using Electric_Meter.MVVM.ViewModels;

namespace Electric_Meter.MVVM.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView(DashboardViewModel dashboardViewModel)
        {
            InitializeComponent();
            DataContext = dashboardViewModel;
        }
    }
}
