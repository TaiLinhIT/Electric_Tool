using System.IO.Ports;

using Electric_Meter.Configs;
using Electric_Meter.Models;
using Electric_Meter.MVVM.ViewModels;
using Electric_Meter.MVVM.Views;
using Electric_Meter.Services;

using Microsoft.Data.SqlClient; // Cần thiết để bắt SqlException
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
                        sqlOptions.CommandTimeout(240); // ⏱ Timeout 60 giây
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

            // Services
            services.AddSingleton<Service>();
            services.AddSingleton<SerialPort>();//new add
            services.AddSingleton<MySerialPortService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddNavigationViewPageProvider();             // từ Wpf.Ui.DependencyInjection
            services.AddSingleton<INavigationService, NavigationService>(); // từ Wpf.Ui (core)
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
        /// Áp dụng migration và seed dữ liệu mặc định khi chạy app
        /// </summary>
        private void ApplyMigrationsAndSeed()
        {
            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();

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
                        catch (SqlException sqlEx) when (sqlEx.Message.Contains("已存在") || sqlEx.Message.Contains("already exists"))
                        {
                            Console.WriteLine("WARNING: Migration failed due to 'object already exists' error. Assuming schema is mostly correct and continuing.");
                            // In lỗi chi tiết
                            Console.WriteLine($"  Inner Exception: {sqlEx.Message}");
                        }
                    }

                    // 2. Seed Data
                    //var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                    await seeder.SeedAsync();

                    // 3. Create Stored Procedures
                    await EnsureStoredProceduresAsync(db);
                }
                catch (Exception ex)
                {
                    // Log tất cả các lỗi khác (Seeding, Stored Procedure, hoặc Migration lỗi khác)
                    Console.WriteLine("❌ Database Startup Error:");
                    Console.WriteLine($"  Message: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"  Inner Exception: {ex.InnerException.Message}");

                    // Throw the exception again to signal a critical startup failure
                    throw;
                }
            }).Wait();
        }

        /// <summary>
        /// Tạo hoặc cập nhật Stored Procedures.
        /// (Đã xóa try-catch ở đây để lỗi SQL được xử lý ở ApplyMigrationsAndSeed)
        /// </summary>
        private static async Task EnsureStoredProceduresAsync(PowerTempWatchContext db)
        {
            var sqlLatestSensor = @"
                    CREATE OR ALTER PROCEDURE GetLatestSensorByDevice
                        @devid INT
                    AS
                    BEGIN
                        SET NOCOUNT ON;

                        ;WITH LatestData AS
                        (
                            SELECT 
                                devid,
                                codeid,
                                value,
                                day,
                                logid,
                                ROW_NUMBER() OVER (
                                    PARTITION BY devid, codeid 
                                    ORDER BY logid DESC
                                ) AS rn
                            FROM SensorData
                            WHERE devid = @devid
                        )
                        SELECT 
                            devid,
                            codeid,
                            value,
                            day,
                            logid -- Đã bổ sung
                        FROM LatestData
                        WHERE rn = 1
                        ORDER BY codeid;
                    END
                ";
            var sqlLatestSensorEachDevice = @"
                CREATE OR ALTER PROCEDURE GetLatestSensorByDeviceYear
                @year INT
            AS
            BEGIN
                SET NOCOUNT ON;

                ;WITH LatestData AS (
                    SELECT 
                        sd.devid,
                        sd.codeid,
                        sd.value,
                        sd.day,
                        ROW_NUMBER() OVER(
                            PARTITION BY sd.devid, sd.codeid
                            ORDER BY sd.day DESC
                        ) AS rn
                    FROM SensorData sd
                    WHERE YEAR(sd.day) = @year
                )
                SELECT 
                    ld.devid,
                    d.name AS device_name,
                    ROUND(
                        SUM(CASE WHEN c.name = 'Imp' THEN ld.value ELSE 0 END) +
                        SUM(CASE WHEN c.name = 'Exp' THEN ld.value ELSE 0 END)
                    , 2) AS TotalValue
                FROM LatestData ld
                JOIN controlcode c 
                    ON ld.codeid = c.codeid
                JOIN devices d
                    ON ld.devid = d.devid
                WHERE ld.rn = 1
                  AND c.name IN ('Imp','Exp')
                GROUP BY ld.devid, d.name
                ORDER BY ld.devid;

            END


            ";
            await db.Database.ExecuteSqlRawAsync(sqlLatestSensor);
            await db.Database.ExecuteSqlRawAsync(sqlLatestSensorEachDevice);
            Console.WriteLine("Stored Procedure successfully.");
        }


    }
}
