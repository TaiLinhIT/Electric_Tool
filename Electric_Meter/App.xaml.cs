using System.Windows;

using Microsoft.Extensions.DependencyInjection;

using Wpf.Ui.Appearance;

namespace Electric_Meter
{
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                // 1️⃣ Khởi tạo Startup để lấy ServiceProvider
                var startup = new Startup();
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                _serviceProvider = startup.ServiceProvider;

                // 2️⃣ Tạo scope và chạy DatabaseSeeder
                //using (var scope = _serviceProvider.CreateScope())
                //{
                //    var seeder = scope.ServiceProvider.GetService<DatabaseSeeder>();
                //    if (seeder != null)
                //    {
                //       // await seeder.SeedAsync(); // chạy seed khi khởi động app
                //    }
                //}

                // 3️⃣ Mở MainWindow sau khi seed xong
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                //var dashborad = _serviceProvider.GetRequiredService<DashboardView>();
                //mainWindow.MainContent.Content = dashboard;

                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Lỗi khi khởi động ứng dụng:\n{ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

    }
}
