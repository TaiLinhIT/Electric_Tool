using System.IO;
using System.IO.Ports;

using Electric_Meter.Configs;
using Electric_Meter.Models;
using Electric_Meter.MVVM.ViewModels;
using Electric_Meter.MVVM.Views;
using Electric_Meter.Services;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection; // BẮT BUỘC

using Wpf.Ui;
using Wpf.Ui.DependencyInjection;


namespace Electric_Meter
{
    public class Startup
    {

        public IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }

        public Startup()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsetting.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            // Áp dụng migration và seed (async)
            //ApplyMigrationsAndSeed(); // Đảm bảo đã mở comment để chạy migration
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Lấy AppSettings
            var appSetting = Configuration.GetSection("AppSettings").Get<AppSetting>()
                ?? throw new NullReferenceException("AppSettings is null. Please check your appsetting.json file.");

            services.AddSingleton(appSetting);

            services.AddSingleton<IConfiguration>(Configuration);

            // 1. Đăng ký Factory để tạo DbContext với chuỗi kết nối động/mới nhất.
            services.AddSingleton<Interfaces.IDbContextFactory, CustomDbContextFactory>();

            // 2. Đăng ký DbContext với Transient/Scoped để đảm bảo nó được tạo ra qua Factory
            // Note: Chúng ta sẽ dùng Factory để tạo thủ công ở Migration và các Repository.

            // Seeder (dùng DI để lấy context tự động)
            // (Sẽ cần cập nhật DatabaseSeeder để nhận IDbContextFactory)
            services.AddTransient<DatabaseSeeder>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SettingViewModel>();
            services.AddSingleton<ToolViewModel>();
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<SystemOfParameterViewModel>();
            services.AddSingleton<ActiveManagerViewModel>();
            services.AddSingleton<CommandManagerViewModel>();
            services.AddSingleton<DeviceManagerViewModel>();
            services.AddSingleton<SensorTypeManagerViewModel>();
            services.AddSingleton<HardwareViewModel>();
            services.AddSingleton<ResetPasswordViewModel>();

            // ĐĂNG KÝ HTTPCLIENT và SERVICE (Service giờ đây tự xử lý lưu/tải config DB)
            services.AddHttpClient<Interfaces.IService, Service>(client =>
            {
                client.BaseAddress = new Uri(appSetting.ApiBaseUrl);
            });
            // Nếu Service có logic ngoài HttpClient (như Load/Save DB), cần đăng ký thêm.
            //services.AddSingleton<Interfaces.IService, Service>(); 

            // Services
            services.AddSingleton<SerialPort>();
            services.AddSingleton<MySerialPortService>();
            services.AddSingleton<Interfaces.IDbContextFactory, CustomDbContextFactory>();

            // ĐĂNG KÝ WPF.UI CORE SERVICES VỚI TÊN ĐẦY ĐỦ (Wpf.Ui.Services.*)
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<Interfaces.IRequestQueueService, RequestQueueService>();
            services.AddNavigationViewPageProvider();
            services.AddSingleton<LanguageService>();

            // UI 
            services.AddSingleton<MainWindow>();
            services.AddSingleton<SettingView>();
            services.AddSingleton<DashboardView>();
            services.AddSingleton<ToolView>();
            services.AddSingleton<SystemOfParameterView>();
            services.AddSingleton<ResetpasswordView>();
            services.AddSingleton<HardwareSetting>();
            services.AddSingleton<ActiveManagerView>();
            services.AddSingleton<CommandManagerView>();
            services.AddSingleton<SensorTypeManagerView>();
            services.AddSingleton<DeviceManagerView>();
        }

        /// <summary>
        /// Đọc nội dung file SQL từ thư mục 'Sql' nằm cùng cấp với file thực thi.
        /// </summary>
        private static string ReadSqlFile(string fileName)
        {
            // Tạo đường dẫn tuyệt đối đến file: [AppBaseDir]/Sql/[fileName]
            var filePath = Path.Combine(AppContext.BaseDirectory, "Sql", fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"ERROR: SQL file not found at path: {filePath}");
                return string.Empty;
            }

            return File.ReadAllText(filePath);
        }

        /// <summary>
        /// Áp dụng migration và seed dữ liệu mặc định khi chạy app
        /// </summary>
        private void ApplyMigrationsAndSeed()
        {
            // Chặn luồng startup cho đến khi database sẵn sàng.
            Task.Run(async () =>
            {
                using var scope = ServiceProvider.CreateScope();

                // --------------------------------------------------------------------------------
                // THAY ĐỔI QUAN TRỌNG: LẤY DB CONTEXT QUA FACTORY
                // --------------------------------------------------------------------------------
                var dbFactory = scope.ServiceProvider.GetRequiredService<Interfaces.IDbContextFactory>();
                using var db = dbFactory.CreateDbContext(); // Sử dụng chuỗi kết nối động/mới nhất

                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                try
                {
                    // 1. Apply Migrations 
                    var pending = await db.Database.GetPendingMigrationsAsync();
                    if (pending.Any())
                    {
                        try
                        {
                            await db.Database.MigrateAsync();
                        }
                        catch (SqlException sqlEx)
                        {
                            Console.WriteLine("WARNING: Migration failed due to 'object already exists' error. Assuming schema is mostly correct and continuing.");
                            Console.WriteLine($" Inner Exception: {sqlEx.Message}");
                        }
                    }

                    // 2. Seed Data
                    //await seeder.SeedAsync(); // Nếu Seeder đã được cập nhật để dùng IDbContextFactory

                    // 3. Create Stored Procedures
                    await EnsureStoredProceduresAsync(db);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Database Startup Error:");
                    Console.WriteLine($" Message: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($" Inner Exception: {ex.InnerException.Message}");

                    throw;
                }
            }).Wait();
        }
        private static async Task EnsureStoredProceduresAsync(PowerTempWatchContext db)
        {
            Console.WriteLine("Creating/Updating Stored Procedures from .sql files...");

            // 1. Đọc nội dung từ file GetLatestSensorByDevice.sql
            var sqlLatestSensor = ReadSqlFile("GetLatestSensorByDevice.sql");
            if (!string.IsNullOrEmpty(sqlLatestSensor))
            {
                await db.Database.ExecuteSqlRawAsync(sqlLatestSensor);
            }

            // 2. Đọc nội dung từ file GetLatestSensorByDeviceYear.sql
            var sqlLatestSensorEachDevice = ReadSqlFile("GetLatestSensorByDeviceYear.sql");
            if (!string.IsNullOrEmpty(sqlLatestSensorEachDevice))
            {
                await db.Database.ExecuteSqlRawAsync(sqlLatestSensorEachDevice);
            }

            // 3. Đọc nội dung từ file Sensordata12monthbydevid.sql
            var sqlSensorData12Month = ReadSqlFile("Sensordata12monthbydevid.sql");
            if (!string.IsNullOrEmpty(sqlSensorData12Month))
            {
                await db.Database.ExecuteSqlRawAsync(sqlSensorData12Month);
            }

            var sqlSensorDataByDateRange = ReadSqlFile("GetRatioMonthlyDevice.sql");
            if (!string.IsNullOrEmpty(sqlSensorDataByDateRange))
            {
                await db.Database.ExecuteSqlRawAsync(sqlSensorDataByDateRange);
            }
            var sqlCauculateDailyConsumption = ReadSqlFile("GetDailyConsumption.sql");
            if (!string.IsNullOrEmpty(sqlCauculateDailyConsumption))
            {
                await db.Database.ExecuteSqlRawAsync(sqlCauculateDailyConsumption);
            }

            Console.WriteLine("Stored Procedure successfully created/updated.");
        }
    }
}
