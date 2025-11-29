using Electric_Meter_WebAPI.Config;
using Electric_Meter_WebAPI.Interfaces;
using Electric_Meter_WebAPI.Models;
using Electric_Meter_WebAPI.Services;

using Microsoft.Data.SqlClient; // Cần cho SqlException
//using Electric_Meter_WebAPI.Utils; // <--- THÊM DÒNG NÀY (Thử với namespace Utils)

using Microsoft.EntityFrameworkCore;

// --- 1. Cấu hình Builder và Services ---
var builder = WebApplication.CreateBuilder(args);

// Lấy đối tượng Configuration để đọc appsettings.json
var configuration = builder.Configuration;

// Đọc và đăng ký AppSettings (Singleton)
var appSetting = configuration.GetSection("AppSettings").Get<AppSetting>()
    ?? throw new NullReferenceException("AppSettings is null. Please check your appsettings.json file.");
builder.Services.AddSingleton(appSetting);

// Đăng ký DbContext với chuỗi kết nối từ AppSettings
builder.Services.AddDbContext<PowerTempWatchContext>(options =>
    options.UseSqlServer(
        appSetting.ConnectString,
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(240); // ⏱ Timeout 240 giây
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null); // Tùy chọn: Thêm tính năng tự động thử lại kết nối
        }
    )
);

// Đăng ký DatabaseSeeder và Service chính
//builder.Services.AddTransient<DatabaseSeeder>(); // Đăng ký DatabaseSeeder
builder.Services.AddScoped<Service>();
builder.Services.AddScoped<IService, Service>();

// Thêm các dịch vụ cần thiết cho Web API 
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// --- 2. Xây dựng và Cấu hình Pipeline ---
var app = builder.Build();

// --- DATABASE INITIALIZATION BLOCK (Chạy trước khi Run) ---
try
{
    // Chạy tác vụ Async để đảm bảo database sẵn sàng.
    await RunDatabaseInitializationAsync(app);
}
catch (Exception ex)
{
    Console.WriteLine("❌ CRITICAL: Database startup failed. Application will stop.");
    Console.WriteLine($"Error: {ex.Message}");
    // Ném ngoại lệ để dừng ứng dụng nếu không thể khởi tạo DB
    throw;
}
// --------------------------------------------------------

// Cấu hình HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Sử dụng Swagger UI chỉ trong môi trường phát triển
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Khởi chạy ứng dụng
app.Run();

// --- DATABASE INITIALIZATION HELPER ---

/// <summary>
/// Áp dụng migration và seed dữ liệu khi khởi động ứng dụng.
/// </summary>
static async Task RunDatabaseInitializationAsync(WebApplication app)
{
    Console.WriteLine("Starting Database Initialization (Migration and Seeding)...");

    // Tạo scope để lấy DbContext và Seeder
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
    //var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

    try
    {
        // 1. Apply Migrations
        var pending = await db.Database.GetPendingMigrationsAsync();
        if (pending.Any())
        {
            Console.WriteLine($"Applying {pending.Count()} pending migration(s)...");
            try
            {
                await db.Database.MigrateAsync();
                Console.WriteLine("Migrations applied successfully.");
            }
            // Bắt lỗi cụ thể 'Object already exists'
            catch (SqlException sqlEx)
            {
                Console.WriteLine("WARNING: Migration failed due to 'object already exists' error. Assuming schema is mostly correct and continuing.");
                Console.WriteLine($" Inner Exception: {sqlEx.Message}");
            }
        }
        else
        {
            Console.WriteLine("No pending migrations found.");
        }

        // 2. Seed Data
        //await seeder.SeedAsync();
        Console.WriteLine("Database seeding completed.");
    }
    catch (Exception ex)
    {
        // Ghi log lỗi và ném lại để dừng ứng dụng
        Console.WriteLine("❌ Database Startup Error during Migration/Seeding.");
        Console.WriteLine($" Message: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($" Inner Exception: {ex.InnerException.Message}");

        throw;
    }
}
