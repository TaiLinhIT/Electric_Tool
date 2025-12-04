using System.Net.Http;
using System.Text;
using System.Windows; // Dành cho WPF MessageBox

using Electric_Meter.Dto;
using Electric_Meter.Dto.ActiveTypeDto;
using Electric_Meter.Dto.CodeTypeDto;
using Electric_Meter.Dto.ControlcodeDto;
using Electric_Meter.Dto.DeviceDto;
using Electric_Meter.Dto.SensorDataDto;
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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IRequestQueueService _requestQueueService;
        private readonly HttpClient _httpClient;

        // Dùng SemaphoreSlim để kiểm soát truy cập DB khi insert SensorData
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public Service(IServiceScopeFactory serviceScope, HttpClient httpClient, IRequestQueueService requestQueueService)
        {
            _httpClient = httpClient;
            _scopeFactory = serviceScope;
            _requestQueueService = requestQueueService;
        }

        // --- Các hàm tiện ích (Utilities) ---

        public string ConvertToHex(int number)
        {
            return number.ToString("X");
        }

        public byte[] ConvertHexStringToByteArray(string hexString)
        {
            hexString = hexString.Replace(" ", "").ToUpper();
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

        // --- Thao tác Database cục bộ (Local DB Operations) ---

        public async Task<bool> InsertToSensorDataAsync(SensorDataDto dto)
        {
            try
            {
                var json = JsonConvert.SerializeObject(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/SensorData/", content);

                if (!response.IsSuccessStatusCode)
                {
                    var respContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Gửi dữ liệu thất bại. Status: {response.StatusCode}, Response: {respContent}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException httpEx)
            {
                Tool.LogHttpRequestException("api/SensorData/", httpEx);
                // Ghi vào hàng đợi để thử lại sau
                await _requestQueueService.EnqueueRequestAsync(HttpMethod.Post, "api/SensorData", dto);
                return true; // Trả về true vì yêu cầu đã được lưu để đồng bộ sau
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return false;
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
                                    name = x.name,
                                    active = y.name,
                                    type = z.name,
                                    ifshow = x.ifshow
                                };
                return lstDevice.ToList();
            }
            catch (Exception ex)
            {
                // Trong ứng dụng WPF/Desktop, MessageBox.Show là chấp nhận được cho các lỗi quan trọng.
                MessageBox.Show(ex.Message);
                return new List<DeviceVM>();
            }
        }

        // --- Thao tác Stored Procedure (Local DB Stored Procedure) ---

        public async Task<List<LatestSensorByDeviceYear>> GetLatestSensorByDeviceYear(int year)
        {
            using var scope = _scopeFactory.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
            var yearParam = new SqlParameter("@year", year);
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

        // --- Thao tác API (Remote API Operations - Device CRUD) ---

        public async Task<List<DeviceDto>> GetListDeviceAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Device/");
                response.EnsureSuccessStatusCode(); // Ném HttpRequestException nếu status code không thành công.
                var content = await response.Content.ReadAsStringAsync();
                var devices = JsonConvert.DeserializeObject<List<DeviceDto>>(content);
                return devices ?? new List<DeviceDto>();
            }
            catch (HttpRequestException httpEx)
            {
                Tool.LogHttpRequestException("api/Device/", httpEx);
                return new List<DeviceDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return new List<DeviceDto>();
            }
        }

        public async Task<DeviceDto> GetDeviceByDevidAsync(int devid)
        {
            try
            {
                // ⭐ SỬA LỖI CÚ PHÁP: Dùng chuỗi nội suy để truyền devid vào URL
                var response = await _httpClient.GetAsync($"api/Device/{devid}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var devices = JsonConvert.DeserializeObject<DeviceDto>(content);
                return devices ?? new DeviceDto();
            }
            catch (HttpRequestException httpEx)
            {
                Tool.LogHttpRequestException($"api/Device/{devid}", httpEx);
                return new DeviceDto();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return new DeviceDto();
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
                Tool.LogHttpRequestException("api/Device/", httpEx);
                // Ghi vào hàng đợi để thử lại sau
                await _requestQueueService.EnqueueRequestAsync(HttpMethod.Post, "api/Device", dto);
                return true; // Trả về true vì yêu cầu đã được lưu để đồng bộ sau
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return false;
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
                Tool.LogHttpRequestException("api/Device/", httpEx);
                // Ghi vào hàng đợi để thử lại sau
                await _requestQueueService.EnqueueRequestAsync(HttpMethod.Put, "api/Device", dto);
                return true; // Trả về true vì yêu cầu đã được lưu để đồng bộ sau
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sửa dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteDeviceAsync(int devid)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/Device/{devid}");

                if (!response.IsSuccessStatusCode)
                {
                    var respContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Xóa dữ liệu thất bại. Status: {response.StatusCode}, Response: {respContent}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException httpEx)
            {
                Tool.LogHttpRequestException($"api/Device/{devid}", httpEx);
                // Ghi vào hàng đợi để thử lại sau. (Dùng devid làm payload để BackgroundSync có thể xử lý)
                await _requestQueueService.EnqueueRequestAsync(HttpMethod.Delete, $"api/Device/{devid}", devid);
                return true; // Trả về true vì yêu cầu đã được lưu để đồng bộ sau
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sửa dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return false;
            }
        }

        // --- Thao tác API (Remote API Operations - ControlCode CRUD) ---

        public async Task<bool> CreateControlcodeAsync(CreateControlcodeDto dto)
        {
            try
            {
                var json = JsonConvert.SerializeObject(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/Controlcode/", content);

                if (!response.IsSuccessStatusCode)
                {
                    var respContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Gửi dữ liệu thất bại. Status: {response.StatusCode}, Response: {respContent}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException httpEx)
            {
                Tool.LogHttpRequestException("api/Controlcode/", httpEx);
                // Ghi vào hàng đợi để thử lại sau
                await _requestQueueService.EnqueueRequestAsync(HttpMethod.Post, "api/Controlcode", dto);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateControlcodeAsync(EditControlcodeDto dto)
        {
            try
            {
                var json = JsonConvert.SerializeObject(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync("api/Controlcode/", content);

                if (!response.IsSuccessStatusCode)
                {
                    var respContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Sửa dữ liệu thất bại. Status: {response.StatusCode}, Response: {respContent}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException httpEx)
            {
                Tool.LogHttpRequestException("api/Controlcode/", httpEx);
                // Ghi vào hàng đợi để thử lại sau
                await _requestQueueService.EnqueueRequestAsync(HttpMethod.Put, "api/Controlcode", dto);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sửa dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteControlcodeAsync(int codeid)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/Controlcode/{codeid}");

                if (!response.IsSuccessStatusCode)
                {
                    var respContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Xóa dữ liệu thất bại. Status: {response.StatusCode}, Response: {respContent}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException httpEx)
            {
                Tool.LogHttpRequestException($"api/Controlcode/{codeid}", httpEx);
                // Ghi vào hàng đợi để thử lại sau
                await _requestQueueService.EnqueueRequestAsync(HttpMethod.Delete, $"api/Controlcode/{codeid}", codeid);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sửa dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return false;
            }
        }

        public async Task<List<ControlcodeDto>> GetListControlcodeAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Controlcode/");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var controlcodes = JsonConvert.DeserializeObject<List<ControlcodeDto>>(content);
                return controlcodes ?? new List<ControlcodeDto>();
            }
            catch (HttpRequestException httpEx)
            {
                Tool.LogHttpRequestException("api/Controlcode/", httpEx);
                return new List<ControlcodeDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return new List<ControlcodeDto>();
            }
        }

        // --- Thao tác API (Remote API Operations - Other Gets) ---

        public async Task<List<CodeTypeDto>> GetCodeTypeAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/CodeType/");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var activetypes = JsonConvert.DeserializeObject<List<CodeTypeDto>>(content);
                return activetypes ?? new List<CodeTypeDto>();
            }
            catch (HttpRequestException httpEx)
            {
                Tool.LogHttpRequestException("api/CodeType/", httpEx);
                return new List<CodeTypeDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return new List<CodeTypeDto>();
            }
        }

        public async Task<List<ActiveTypeDto>> GetActiveTypesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/ActiveType/");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var activetypes = JsonConvert.DeserializeObject<List<ActiveTypeDto>>(content);
                return activetypes ?? new List<ActiveTypeDto>();
            }
            catch (HttpRequestException httpEx)
            {
                Tool.LogHttpRequestException("api/ActiveType/", httpEx);
                return new List<ActiveTypeDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return new List<ActiveTypeDto>();
            }
        }

        public async Task<List<SensorTypeDto>> GetSensorTypesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/SensorType/");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var sensortypes = JsonConvert.DeserializeObject<List<SensorTypeDto>>(content);
                return sensortypes ?? new List<SensorTypeDto>();
            }
            catch (HttpRequestException httpEx)
            {
                Tool.LogHttpRequestException("api/SensorType/", httpEx);
                return new List<SensorTypeDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return new List<SensorTypeDto>();
            }
        }

        public async Task<SensorTypeDto> GetSensorTypeByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/SensorType/{id}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var sensortypes = JsonConvert.DeserializeObject<SensorTypeDto>(content);
                return sensortypes ?? new SensorTypeDto();
            }
            catch (HttpRequestException httpEx)
            {
                Tool.LogHttpRequestException($"api/SensorType/{id}", httpEx);
                return new SensorTypeDto();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return new SensorTypeDto();
            }
        }

        public async Task<ControlcodeDto> GetControlcodeByDevidAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Controlcode/{id}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var controlcode = JsonConvert.DeserializeObject<ControlcodeDto>(content);
                return controlcode ?? new ControlcodeDto();
            }
            catch (HttpRequestException httpEx)
            {
                Tool.LogHttpRequestException($"api/Controlcode/{id}", httpEx);
                return new ControlcodeDto();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gửi dữ liệu thất bại (Lỗi chung): {ex.Message}");
                return new ControlcodeDto();
            }
        }

        public Task<ControlcodeDto> GetControlcodeByDevidAsync()
        {
            throw new NotImplementedException();
        }
    }
}
