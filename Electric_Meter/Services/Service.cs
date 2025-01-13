using Electric_Meter.Interfaces;
using Electric_Meter.Models;
using Electric_Meter.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

        public async Task<int> DeleteToMachine(Electric_Meter.Models.Machine machine)
        {
            try
            {
                _context.machines.Remove(machine);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
                return 0;
            }
        }

        public async Task<int> EditToMachine(Electric_Meter.Models.Machine machine)
        {
            try
            {
                _context.machines.Update(machine);
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
            using(var scope = _scopeFactory.CreateScope())
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
            using(var scope = _scopeFactory.CreateScope())
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



        public async Task<int> InsertToMachine(Electric_Meter.Models.Machine machine)
        {
            try
            {
                await _context.machines.AddAsync(machine);
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

    }


}
