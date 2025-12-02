using System.Net.Http;
using System.Text;
using System.Windows;

using Electric_Meter.Dto;
using Electric_Meter.Dto.ActiveTypeDto;
using Electric_Meter.Dto.DeviceDto;
using Electric_Meter.Dto.SensorTypeDto;
using Electric_Meter.Interfaces;
using Electric_Meter.Models;
using Electric_Meter.Utilities;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

namespace Electric_Meter.Services
{
    public class Service : IService
    {

        private readonly PowerTempWatchContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HttpClient _httpClient;
        public Service(IServiceScopeFactory serviceScope, HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Cập nhật từ HTTP sang HTTPS
            _httpClient.BaseAddress = new Uri("https://localhost:7099");
            _scopeFactory = serviceScope;
        }

        public async Task<int> DeleteToDeviceAsync(Device device)
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

                MessageBox.Show(ex.Message);
                return 0;
            }
        }

        public async Task<int> EditToDeviceAsync(Device device)
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
                MessageBox.Show(ex.Message);
                return 0;
            }


        }

        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public async Task<int> InsertToDeviceAsync(Device device)
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
                    Tool.Log($"→ Bắt đầu thêm SensorData cho codeid {data.codeid}");
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

        public List<DeviceVM> GetDevicesList()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var lstDevice = from x in _context.devices
                                join y in _context.activeTypes on x.activeid equals y.activeid
                                join z in _context.sensorTypes on x.typeid equals z.typeid
                                select new DeviceVM
                                {
                                    devid = x.devid,
                                    //address = x.address,
                                    name = x.name,
                                    //port = x.port,
                                    //assembling = x.assembling,
                                    //baudrate = x.baudrate,
                                    active = y.name,
                                    type = z.name,
                                    ifshow = x.ifshow
                                };
                return lstDevice.ToList();

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
                return new List<DeviceVM>();
            }
        }
        public List<ControlcodeVM> GetControlCodeListByDevid(int devid)
        {
            using var scope = _scopeFactory.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
            var lstControlCode = from x in _context.controlcodes
                                 join y in _context.devices on x.devid equals y.devid
                                 join z in _context.codetypes on x.codetypeid equals z.codetypeid
                                 join g in _context.activeTypes on x.activeid equals g.activeid
                                 join h in _context.sensorTypes on x.typeid equals h.typeid
                                 where x.devid == devid && x.activeid == 1
                                 select new ControlcodeVM
                                 {
                                     codeid = x.codeid,
                                     devid = x.devid,
                                     deviceName = y.name,
                                     code = x.code,
                                     active = g.name,
                                     codetype = z.name,
                                     name = x.name,
                                     factor = x.factor,
                                     type = h.name,
                                     high = x.high,
                                     low = x.low,
                                     ifshow = x.ifshow,
                                     ifcal = x.ifcal
                                 };
            return lstControlCode.ToList();
        }

        //public List<Device> GetDevicesByAssembling(string key)
        //{
        //    using var scope = _scopeFactory.CreateScope();
        //    var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
        //    return _context.devices.Where(x => x.assembling.Contains(key) && x.activeid == 1 && x.typeid == 7).ToList();
        //}

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

        public async Task<int> InsertToControlcodeAsync(Controlcode code)
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

                MessageBox.Show(ex.Message);
                return 0;
            }

        }

        public async Task<int> EditToControlcodeAsync(Controlcode code)
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

                MessageBox.Show(ex.Message);
                return 0;
            }
        }

        public async Task<int> DeleteToControlcodeAsync(Controlcode code)
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

                MessageBox.Show(ex.Message);
                return 0;
            }
        }

        public async Task<bool> CreateDeviceAsync(CreateDeviceDto dto)
        {
            try
            {
                var json = JsonConvert.SerializeObject(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/Device/", content);

                if (!response.IsSuccessStatusCode)
                {
                    var respContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Gửi dữ liệu thất bại. Status: {response.StatusCode}, Response: {respContent}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException httpEx)
            {
                // Bắt lỗi kết nối mạng. Sửa thông báo để hướng dẫn người dùng kiểm tra HTTPS.
                Console.WriteLine($"Gửi dữ liệu thất bại. Lỗi: {httpEx.Message}");

                if (httpEx.InnerException != null)
                {
                    Console.WriteLine($"Loại: {httpEx.InnerException.GetType().Name}, Message: {httpEx.InnerException.Message}");
                }

                if (httpEx.InnerException is System.Security.Authentication.AuthenticationException)
                {
                    Console.WriteLine("Lỗi SSL: Cần cấu hình HttpClient để bỏ qua lỗi chứng chỉ tự ký (self-signed) cho localhost.");
                }
                return false;
            }
            catch (Exception ex)
            {
                // Bắt các lỗi khác
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return false;
            }
        }

        public async Task<List<DeviceDto>> GetListDeviceAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Device/");
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Không thể kết nối đến API Device.");
                }
                var content = await response.Content.ReadAsStringAsync();
                var devices = JsonConvert.DeserializeObject<List<DeviceDto>>(content);
                return devices ?? new List<DeviceDto>();
            }
            catch (HttpRequestException httpEx)
            {

                Console.WriteLine($"Gửi dữ liệu thất bại. Lỗi: {httpEx.Message}");

                if (httpEx.InnerException != null)
                {
                    Console.WriteLine($"Loại: {httpEx.InnerException.GetType().Name}, Message: {httpEx.InnerException.Message}");
                }


                if (httpEx.InnerException is System.Security.Authentication.AuthenticationException)
                {
                    Console.WriteLine("Lỗi SSL: Cần cấu hình HttpClient để bỏ qua lỗi chứng chỉ tự ký (self-signed) cho localhost.");
                }
                return new List<DeviceDto>();
            }
            catch (Exception ex)
            {
                // Bắt các lỗi khác
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return new List<DeviceDto>();
            }
        }

        public async Task<List<ActiveTypeDto>> GetActiveTypesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/ActiveType/");


                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Không thể kết nối đến API Device.");
                }
                var content = await response.Content.ReadAsStringAsync();
                var activetypes = JsonConvert.DeserializeObject<List<ActiveTypeDto>>(content);
                return activetypes ?? new List<ActiveTypeDto>();
            }

            catch (HttpRequestException httpEx)
            {

                Console.WriteLine($"Gửi dữ liệu thất bại. Lỗi: {httpEx.Message}");

                if (httpEx.InnerException != null)
                {
                    Console.WriteLine($"Loại: {httpEx.InnerException.GetType().Name}, Message: {httpEx.InnerException.Message}");
                }


                if (httpEx.InnerException is System.Security.Authentication.AuthenticationException)
                {
                    Console.WriteLine("Lỗi SSL: Cần cấu hình HttpClient để bỏ qua lỗi chứng chỉ tự ký (self-signed) cho localhost.");
                }
                return new List<ActiveTypeDto>();
            }
            catch (Exception ex)
            {
                // Bắt các lỗi khác
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return new List<ActiveTypeDto>();
            }
        }

        public async Task<List<SensorTypeDto>> GetSensorTypesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/SensorType/");


                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Không thể kết nối đến API Device.");
                }
                var content = await response.Content.ReadAsStringAsync();
                var sensortypes = JsonConvert.DeserializeObject<List<SensorTypeDto>>(content);
                return sensortypes ?? new List<SensorTypeDto>();
            }

            catch (HttpRequestException httpEx)
            {

                Console.WriteLine($"Gửi dữ liệu thất bại. Lỗi: {httpEx.Message}");

                if (httpEx.InnerException != null)
                {
                    Console.WriteLine($"Loại: {httpEx.InnerException.GetType().Name}, Message: {httpEx.InnerException.Message}");
                }


                if (httpEx.InnerException is System.Security.Authentication.AuthenticationException)
                {
                    Console.WriteLine("Lỗi SSL: Cần cấu hình HttpClient để bỏ qua lỗi chứng chỉ tự ký (self-signed) cho localhost.");
                }
                return new List<SensorTypeDto>();
            }
            catch (Exception ex)
            {
                // Bắt các lỗi khác
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return new List<SensorTypeDto>();
            }
        }

        public async Task<bool> UpdateDeviceAsync(EditDeviceDto dto)
        {
            try
            {
                var json = JsonConvert.SerializeObject(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync("api/Device/", content);

                if (!response.IsSuccessStatusCode)
                {
                    var respContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Sửa dữ liệu thất bại. Status: {response.StatusCode}, Response: {respContent}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"Sửa dữ liệu thất bại. Lỗi: {httpEx.Message}");

                if (httpEx.InnerException != null)
                {
                    Console.WriteLine($"Loại: {httpEx.InnerException.GetType().Name}, Message: {httpEx.InnerException.Message}");
                }

                if (httpEx.InnerException is System.Security.Authentication.AuthenticationException)
                {
                    Console.WriteLine("Lỗi SSL: Cần cấu hình HttpClient để bỏ qua lỗi chứng chỉ tự ký (self-signed) cho localhost.");
                }
                return false;
            }
            catch (Exception ex)
            {
                // Bắt các lỗi khác
                Console.WriteLine($"Sửa dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return false;
            }
        }

        public async Task<DeviceDto> GetDeviceByDevidAsync(int devid)
        {

            try
            {
                var response = await _httpClient.GetAsync("api/Device/{devid}");
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Không thể kết nối đến API Device.");
                }
                var content = await response.Content.ReadAsStringAsync();
                var devices = JsonConvert.DeserializeObject<DeviceDto>(content);
                return devices ?? new DeviceDto();
            }
            catch (HttpRequestException httpEx)
            {

                Console.WriteLine($"Gửi dữ liệu thất bại. Lỗi: {httpEx.Message}");

                if (httpEx.InnerException != null)
                {
                    Console.WriteLine($"Loại: {httpEx.InnerException.GetType().Name}, Message: {httpEx.InnerException.Message}");
                }


                if (httpEx.InnerException is System.Security.Authentication.AuthenticationException)
                {
                    Console.WriteLine("Lỗi SSL: Cần cấu hình HttpClient để bỏ qua lỗi chứng chỉ tự ký (self-signed) cho localhost.");
                }
                return new DeviceDto();
            }
            catch (Exception ex)
            {
                // Bắt các lỗi khác
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return new DeviceDto();
            }
        }
    }


}
