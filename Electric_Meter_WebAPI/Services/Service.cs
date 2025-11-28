using Electric_Meter_WebAPI.Dto;
using Electric_Meter_WebAPI.Interfaces;
using Electric_Meter_WebAPI.Models;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Electric_Meter_WebAPI.Services
{
    public class Service : IService
    {

        private readonly PowerTempWatchContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        public Service(PowerTempWatchContext powerTempWatchContext, IServiceScopeFactory serviceScope)
        {
            //_context = powerTempWatchContext;
            _scopeFactory = serviceScope;
        }

        public async Task<int> DeleteToDevice(Device device)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                device.activeid = 0;
                _context.devices.Update(device);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {

                return 0;
            }
        }

        public async Task<int> EditToDevice(Device device)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                _context.devices.Update(device);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {
                return 0;
            }


        }

        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public async Task<int> InsertToDevice(Device device)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                await _context.devices.AddAsync(device);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {
               
                return 0;
            }
        }
        public string ConvertToHex(int number)
        {
            return number.ToString("X");
        }
        // Hàm chuyển chuỗi hex thành mảng byte
        public byte[] ConvertHexStringToByteArray(string hexString)
        {
            hexString = hexString.Replace(" ", "").ToUpper(); // Chuẩn hóa chuỗi
            if (hexString.Length % 2 != 0 || !System.Text.RegularExpressions.Regex.IsMatch(hexString, "^[0-9A-F]+$"))
            {
                throw new FormatException("Invalid hex string.");
            }

            int numberOfBytes = hexString.Length / 2;
            byte[] bytes = new byte[numberOfBytes];
            for (int i = 0; i < numberOfBytes; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        public async Task<bool> InsertToSensorDataAsync(SensorData data)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                await _semaphore.WaitAsync();

                try
                {
                    
                    await dbContext.sensorDatas.AddAsync(data);
                    var result = await dbContext.SaveChangesAsync();

           
                    return result > 0;
                }
                catch (Exception ex)
                {
                    
                    if (ex.InnerException != null)
                    {
                        
                    }
                    return false;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }


        public List<Device> GetDevicesByAssembling(string key)
        {
            using var scope = _scopeFactory.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
            return _context.devices.Where(x => x.assembling.Contains(key) && x.activeid == 1 && x.typeid == 7).ToList();
        }

        public async Task<List<Device>> GetActiveDevicesAsync()
        {
            try
            {
                return await _context.devices
                    .FromSqlRaw("EXEC GetActiveDevices")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ Lỗi khi gọi GetActiveDevices: " + ex.Message);
                return new List<Device>();
            }
        }

        public async Task<List<Device>> GetDeviceByIdAsync(int devid)
        {
            try
            {
                var param = new SqlParameter("@devid", devid);
                return await _context.devices
                    .FromSqlRaw("EXEC GetDeviceById @devid", param)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ Lỗi khi gọi GetDeviceById: " + ex.Message);
                return new List<Device>();
            }
        }
        public async Task<List<LatestSensorByDeviceYear>> GetLatestSensorByDeviceYear(int year)
        {
            using var scope = _scopeFactory.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();

            // 1. Định nghĩa tham số SQL
            var yearParam = new SqlParameter("@year", year);

            // 2. Sử dụng FromSqlRaw
            return await _context.Set<LatestSensorByDeviceYear>()
                .FromSqlRaw("EXEC GetLatestSensorByDeviceYear @year", yearParam)
                .ToListAsync();
        }


        public async Task<List<SensorData>> GetLatestSensorByDeviceAsync(int devid)
        {
            using var scope = _scopeFactory.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();

            return await _context.sensorDatas
            .FromSqlInterpolated($"EXEC GetLatestSensorByDevice @devid={devid}")
            .ToListAsync();
        }

        public async Task<List<DailyConsumptionDTO>> GetDailyConsumptionDTOs(int devid)
        {

            using var scope = _scopeFactory.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
            var devidCurrent = new SqlParameter("@devid", devid);
            return await _context.Set<DailyConsumptionDTO>()
                .FromSqlRaw($"EXEC GetDailyConsumption @devid", devidCurrent)
                .ToListAsync();


        }

        public async Task<List<TotalConsumptionPercentageDeviceDTO>> GetRatioMonthlyDevice(int month, int year)
        {

            using var scope = _scopeFactory.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
            return await _context.Set<TotalConsumptionPercentageDeviceDTO>()
                .FromSqlInterpolated($"EXEC GetRatioMonthlyDevice @month={month}, @year={year}")
                .ToListAsync();

        }

        public async Task<int> InsertToControlcode(Controlcode code)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                await _context.controlcodes.AddAsync(code);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {

                return 0;
            }

        }

        public async Task<int> EditToControlcode(Controlcode code)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                _context.controlcodes.Update(code);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public async Task<int> DeleteToControlcode(Controlcode code)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                code.activeid = 0;
                _context.controlcodes.Update(code);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {

                return 0;
            }
        }


    }
}
