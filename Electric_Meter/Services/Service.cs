using System.Windows;

using Electric_Meter.Interfaces;
using Electric_Meter.Models;
using Electric_Meter.Utilities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Electric_Meter.Services
{
    public class Service : IService
    {

        private readonly PowerTempWatchContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        public Service(PowerTempWatchContext powerTempWatchContext, IServiceScopeFactory serviceScope)
        {
            _context = powerTempWatchContext;
            _scopeFactory = serviceScope;
        }

        public async Task<int> DeleteToDevice(Device device)
        {
            try
            {
                device.activeid = 0;
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
                return 0;
            }
        }

        public async Task<int> EditToDevice(Device device)
        {
            try
            {
                _context.devices.Update(device);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return 0;
            }


        }

        public async Task<List<DvElectricDataTemp>> GetListDataAsync(int address)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                try
                {
                    // Ordering by CreatedAt (assuming the column name is CreatedAt)
                    var data = await dbContext.DvElectricDataTemps.Where(x => x.IdMachine == address)
                                             .OrderByDescending(d => d.UploadDate) // Order by date descending
                                             .Take(1) // Get the most recent 2 records
                                             .ToListAsync();

                    return data;
                }
                catch (Exception ex)
                {

                    Tool.Log(ex.Message);
                    return new List<DvElectricDataTemp>();
                }

            }
        }

        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public async Task InsertToElectricDataTempAsync(DvElectricDataTemp dvElectricDataTemp)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                await _semaphore.WaitAsync();
                try
                {

                    await dbContext.DvElectricDataTemps.AddAsync(dvElectricDataTemp);
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi thêm dữ liệu vào cơ sở dữ liệu: {ex.Message}");
                }
                finally
                {
                    _semaphore.Release();
                }

            }
        }






        public async Task<int> InsertToDevice(Device device)
        {
            try
            {
                await _context.devices.AddAsync(device);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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

                    Tool.Log($"→ SaveChangesAsync: {result} record(s) affected for codeid {data.codeid}");
                    return result > 0;
                }
                catch (Exception ex)
                {
                    Tool.Log($"Lỗi khi lưu SensorData (codeid {data.codeid}): {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Tool.Log($"→ Chi tiết lỗi bên trong: {ex.InnerException.Message}");
                    }
                    return false;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        public  List<Device> GetDevicesList()
        {
            var lstDevice =  _context.devices.Where(x => x.activeid == 1 && x.typeid ==7).ToList();
            return lstDevice;
        }
    }


}
