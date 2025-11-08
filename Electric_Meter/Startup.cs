using Electric_Meter.Configs;
using Electric_Meter.Models;
using Electric_Meter.MVVM.ViewModels;
using Electric_Meter.MVVM.Views;
using Electric_Meter.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Electric_Meter
{
    public class Startup
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }

        public Startup()
        {
            // Đọc file appsetting.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory) // an toàn hơn cho WPF khi publish
                .AddJsonFile("appsetting.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            // Áp dụng migration và seed (async)
            ApplyMigrationsAndSeed();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Lấy AppSettings
            var appSetting = Configuration.GetSection("AppSettings").Get<AppSetting>()
                ?? throw new NullReferenceException("AppSettings is null. Please check your appsetting.json file.");

            services.AddSingleton(appSetting);

            // Đăng ký DbContext
            services.AddDbContext<PowerTempWatchContext>(options =>
                options.UseSqlServer(appSetting.ConnectString));

            // Seeder (dùng DI để lấy context tự động)
            services.AddTransient<DatabaseSeeder>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SettingViewModel>();
            services.AddSingleton<ToolViewModel>();

            // Services
            services.AddSingleton<Service>();
            services.AddSingleton<MySerialPortService>();

            // UI (MainWindow)
            services.AddSingleton<MainWindow>();

            // UI (SettingView)
            services.AddSingleton<SettingView>();
        }

        /// <summary>
        /// Áp dụng migration và seed dữ liệu mặc định khi chạy app
        /// </summary>
        private void ApplyMigrationsAndSeed()
        {
            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();

            Task.Run(async () =>
            {
                try
                {
                    var pending = await db.Database.GetPendingMigrationsAsync();
                    if (pending.Any())
                        await db.Database.MigrateAsync();

                    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                    await seeder.SeedAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Migration/Seeding failed: " + ex.Message);
                }
            }).Wait(); // Wait ở background, không chặn UI thread
        }

    }
}
