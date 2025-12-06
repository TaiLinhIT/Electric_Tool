// File: Electric_Meter.Services/CustomDbContextFactory.cs (ĐÃ SỬA ĐỔI HOÀN CHỈNH)

using System;
using System.IO;
using System.Text.Json;

using Electric_Meter.Interfaces;
using Electric_Meter.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // ⚠️ CẦN THIẾT CHO CẤU HÌNH DỰ PHÒNG

namespace Electric_Meter.Services
{
    public class CustomDbContextFactory : IDbContextFactory
    {
        private const string DbConfigFilePath = "db_config.json";
        private readonly IConfiguration _configuration; // Khai báo IConfiguration

        // ⚠️ Bổ sung Constructor để nhận IConfiguration qua Dependency Injection
        public CustomDbContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Phương thức đọc chuỗi kết nối từ file cục bộ hoặc appsetting.json
        public string GetCurrentConnectionString()
        {
            if (File.Exists(DbConfigFilePath))
            {
                try
                {
                    var json = File.ReadAllText(DbConfigFilePath);
                    var parameters = JsonSerializer.Deserialize<SystemParameter>(json);
                    if (parameters != null)
                    {
                        // Trả về chuỗi kết nối tạo từ parameters đã lưu
                        return $"Server={parameters.BackupDbLocation};Database={parameters.DatabaseName};User ID={parameters.Account};Password={parameters.Password};TrustServerCertificate=True;";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading dynamic config: {ex.Message}. Returning null.");
                    // Bỏ qua lỗi và chuyển sang trả về null
                }
            }

            return null;
        }

        public PowerTempWatchContext CreateDbContext()
        {
            string connectionString = GetCurrentConnectionString();

            // Vẫn cần kiểm tra, nhưng hành động này sẽ THROW EXCEPTION nếu chưa có config
            if (string.IsNullOrEmpty(connectionString))
            {
                // ⚠️ THAY ĐỔI CÁCH BÁO LỖI:
                // Đã loại bỏ chuỗi kết nối dự phòng. Bây giờ, nếu không có config động,
                // chúng ta CHẤP NHẬN BÁO LỖI ĐỂ TẠM DỪNG VIỆC KHỞI TẠO DB.
                throw new InvalidOperationException("Database is not configured. Please enter connection parameters in the settings screen and click Save.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<PowerTempWatchContext>();
            optionsBuilder.UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.CommandTimeout(240);
                }
            );

            return new PowerTempWatchContext(optionsBuilder.Options);
        }
    }
}
