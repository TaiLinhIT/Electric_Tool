using Electric_Meter.MVVM.ViewModels;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls; // Thêm để đảm bảo NavigationView được nhận dạng
using INavigationService = Wpf.Ui.INavigationService;

namespace Electric_Meter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Khai báo các services để lưu trữ sau khi inject
        private readonly INavigationService _navigationService;
        private readonly IPageService _pageService;

        // ✅ CẬP NHẬT CONSTRUCTOR: Thêm INavigationService và IPageService
        public MainWindow(
            MainViewModel viewModel,
            INavigationService navigationService,
            IPageService pageService)             // Service Page Resolution
        {
            // Lưu trữ services
            _navigationService = navigationService;
            _pageService = pageService;

            DataContext = viewModel;
            InitializeComponent();

            Loaded += (sender, args) =>
            {
                Wpf.Ui.Appearance.SystemThemeWatcher.Watch(
                    this,
                    Wpf.Ui.Controls.WindowBackdropType.Mica,
                    true
                );

                // Attach the service to the NavigationView
                navigationService.SetNavigationControl(RootNavigationView);

                // You can also set the page service, which is required for some functionalities
                RootNavigationView.SetPageService(pageService);
            };
        }
        public MainViewModel ViewModel { get; }
    }
}
