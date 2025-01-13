using Electric_Meter.MVVM.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace Electric_Meter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Khởi tạo Startup
            var startup = new Startup();
            _serviceProvider = startup.ServiceProvider;

            // Khởi tạo MainWindow với DI
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }   
}
