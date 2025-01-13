using Electric_Meter.Configs;
using Electric_Meter.Interfaces;
using Electric_Meter.Models;
using Electric_Meter.MVVM.ViewModels;
using Electric_Meter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace Electric_Meter
{
    public class Startup
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }

        public Startup()
        {
            // Đọc cấu hình từ appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Lấy thư mục hiện tại
                .AddJsonFile("appsetting.json", optional: false, reloadOnChange: true); // Tải file appsettings.json

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Bind AppSettings với cấu hình từ appsettings.json
            var appSetting = Configuration.GetSection("AppSettings").Get<AppSetting>();

            if (appSetting == null)
            {
                throw new NullReferenceException("AppSettings is null. Please check your appsetting.json file.");
            }

            // Đăng ký AppSetting
            services.AddSingleton(appSetting);

            // Đăng ký DbContext với chuỗi kết nối từ AppSetting
            services.AddDbContext<PowerTempWatchContext>(options =>
                options.UseSqlServer(appSetting.ConnectString));

            // Đăng ký ViewModels
            services.AddSingleton<MainViewModel>();

            // Đăng ký MainWindow
            services.AddSingleton<MainWindow>();

            // Đăng ký SettingViewModel
            services.AddSingleton<SettingViewModel>();

            //Đăng ký ToolViewModel
            services.AddSingleton<ToolViewModel>();

            //Đăng ký Service
            services.AddSingleton<Service>();

            //Đăng ký MySerialPort
            services.AddSingleton<MySerialPortService>();
        }

        
    }
}
