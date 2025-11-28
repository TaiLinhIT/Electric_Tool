using System.IO;
using System.IO.Ports;
using System.Net.Http;

using Electric_Meter.Configs;
using Electric_Meter.Models;
using Electric_Meter.MVVM.ViewModels;
using Electric_Meter.MVVM.Views;
using Electric_Meter.Services;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                options.UseSqlServer(
                    appSetting.ConnectString,
                    sqlOptions =>
                    {
                        sqlOptions.CommandTimeout(240); // ⏱ Timeout 240 giây
                    }
                )
            );


            // Seeder (dùng DI để lấy context tự động)
            services.AddTransient<DatabaseSeeder>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SettingViewModel>();
            services.AddSingleton<ToolViewModel>();
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<HttpClient>();

            // Services
            services.AddSingleton<Service>();
            services.AddSingleton<SerialPort>();//new add
            services.AddSingleton<MySerialPortService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddNavigationViewPageProvider();            
            services.AddSingleton<LanguageService>();

            // UI (MainWindow)
            services.AddSingleton<MainWindow>();

            // UI (SettingView)
            services.AddSingleton<SettingView>();

            // UI (Dashboard)
            services.AddSingleton<DashboardView>();

            // UI (ToolView)
            services.AddSingleton<ToolView>();
        }

        /// <summary>
        /// Đọc nội dung file SQL từ thư mục 'Sql' nằm cùng cấp với file thực thi.
        /// </summary>
        private static string ReadSqlFile(string fileName)
        {
            // Tạo đường dẫn tuyệt đối đến file: [AppBaseDir]/Sql/[fileName]
            // AppContext.BaseDirectory trỏ đến thư mục bin/Debug/netcoreappX.X/
            var filePath = Path.Combine(AppContext.BaseDirectory, "Sql", fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"ERROR: SQL file not found at path: {filePath}");
                // Vô hiệu hóa tính năng để tránh lỗi crash nếu file không tồn tại
                // Tuy nhiên, nếu Stored Proc là bắt buộc thì nên ném ngoại lệ
                // throw new FileNotFoundException($"SQL file not found at path: {filePath}", filePath);
                return string.Empty; // Trả về chuỗi rỗng để tránh crash và bỏ qua việc tạo SP
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
                var db = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                try
                {
                    // 1. Apply Migrations (Tách ra để xử lý lỗi "Object already exists")
                    var pending = await db.Database.GetPendingMigrationsAsync();
                    if (pending.Any())
                    {
                        try
                        {
                            await db.Database.MigrateAsync();
                        }
                        // Bắt lỗi cụ thể "Đối tượng đã tồn tại" và bỏ qua lỗi này (chỉ trong trường hợp development)
                        catch (SqlException sqlEx)
                        {
                            Console.WriteLine("WARNING: Migration failed due to 'object already exists' error. Assuming schema is mostly correct and continuing.");
                            // In lỗi chi tiết
                            Console.WriteLine($" Inner Exception: {sqlEx.Message}");
                        }
                    }

                    // 2. Seed Data
                    await seeder.SeedAsync();

                    // 3. Create Stored Procedures
                    await EnsureStoredProceduresAsync(db);
                }
                catch (Exception ex)
                {
                    // Log tất cả các lỗi khác (Seeding, Stored Procedure, hoặc Migration lỗi khác)
                    Console.WriteLine("❌ Database Startup Error:");
                    Console.WriteLine($" Message: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($" Inner Exception: {ex.InnerException.Message}");

                    // Throw the exception again to signal a critical startup failure
                    throw;
                }
            }).Wait();
        }

        /// <summary>
        /// Tạo hoặc cập nhật Stored Procedures bằng cách đọc từ các file .sql trong thư mục Sql.
        /// </summary>
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
