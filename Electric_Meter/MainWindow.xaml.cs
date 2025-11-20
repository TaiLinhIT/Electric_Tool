using System.Windows;

using Electric_Meter.MVVM.ViewModels;

using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;


namespace Electric_Meter
{
    public partial class MainWindow : Window
    {
        private readonly INavigationService _navigationService;
        private readonly INavigationViewPageProvider _pageProvider;

        public MainWindow(
            MainViewModel viewModel,
            INavigationService navigationService,
            INavigationViewPageProvider pageProvider)
        {
            InitializeComponent();
            // Ẩn cửa sổ khi chạy app
            this.Hide();
            DataContext = viewModel;
            _navigationService = navigationService;
            _pageProvider = pageProvider;

            Loaded += (_, _) =>
            {
                // Bật hiệu ứng Mica hoặc Acrylic
                SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);

                // Gán NavigationView và Provider cho hệ thống điều hướng
                _navigationService.SetNavigationControl(RootNavigationView);
                RootNavigationView.SetPageProviderService(_pageProvider);
            };
        }
        

    }
}

