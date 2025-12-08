using Electric_Meter_WebAPI.Dto;
using Electric_Meter_WebAPI.Dto.ActiveTypeDto;
using Electric_Meter_WebAPI.Dto.CodeTypeDto;
using Electric_Meter_WebAPI.Dto.ControlcodeDto;
using Electric_Meter_WebAPI.Dto.DeviceDto;
using Electric_Meter_WebAPI.Dto.SensorDataDto;
using Electric_Meter_WebAPI.Dto.SensorTypeDto;
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

            _scopeFactory = serviceScope;

        }



        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

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

        public async Task<bool> InsertToSensorDataAsync(SensorDataDto dto)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                await _semaphore.WaitAsync();

                try
                {
                    var result = new SensorData()
                    {
                        devid = dto.Devid,
                        codeid = dto.Codeid,
                        day = dto.Day,
                        value = dto.Value,
                    };
                    await dbContext.sensorDatas.AddAsync(result);
                    var check = await dbContext.SaveChangesAsync();


                    return check > 0;
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
            //return _context.devices.Where(x => x.assembling.Contains(key) && x.activeid == 1 && x.typeid == 7).ToList();
            return _context.devices.Where(x => x.activeid == 1 && x.typeid == 7).ToList();
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





        public async Task<List<DeviceDto>> GetListDevice()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var results = from x in _context.devices
                              join y in _context.activeTypes on x.activeid equals y.activeid
                              join z in _context.sensorTypes on x.typeid equals z.typeid
                              where (x.ifshow == 1)
                              select new DeviceDto
                              {
                                  devid = x.devid,
                                  name = x.name,
                                  active = y.name,
                                  type = z.name,
                                  ifshow = x.ifshow
                              };
                return await results.ToListAsync();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                return new List<DeviceDto>();
            }
        }
        public async Task<int> InsertToDevice(CreateDeviceDto dto)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                bool check = _context.devices.Any(x => x.devid == dto.devid);
                if (check)
                {
                    Console.WriteLine("device exits");
                    return 0;
                }
                var data = new Device
                {
                    devid = dto.devid,
                    name = dto.name,
                    activeid = _context.activeTypes.Where(x => x.name == dto.active).Select(x => x.activeid).FirstOrDefault(),
                    typeid = _context.sensorTypes.Where(x => x.name == dto.type).Select(x => x.typeid).FirstOrDefault(),
                    ifshow = dto.ifshow
                };
                await _context.devices.AddAsync(data);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {

                return 0;
            }
        }

        public async Task<int> EditDeviceAsync(EditDeviceDto dto)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var findDevice = _context.devices.FirstOrDefault(x => x.devid == dto.devid);
                if (findDevice == null)
                {
                    return 0;
                }
                findDevice.name = dto.name;
                findDevice.typeid = _context.sensorTypes.Where(x => x.name == dto.type).Select(x => x.typeid).FirstOrDefault();
                findDevice.activeid = _context.activeTypes.Where(x => x.name == dto.active).Select(x => x.activeid).FirstOrDefault();
                findDevice.ifshow = dto.ifshow;

                _context.devices.Update(findDevice);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }
        public async Task<int> DeleteDeviceAsync(int devid)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var findDevice = _context.devices.FirstOrDefault(x => x.devid == devid);
                if (findDevice == null)
                {
                    return 0;
                }

                findDevice.ifshow = 0;

                _context.devices.Update(findDevice);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<DeviceDto> GetDeviceByDevid(int devid)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var results = await (from x in _context.devices
                                     where x.devid == devid
                                     join y in _context.activeTypes on x.activeid equals y.activeid
                                     join z in _context.sensorTypes on x.typeid equals z.typeid
                                     select new DeviceDto
                                     {
                                         devid = x.devid,
                                         name = x.name,
                                         active = y.name,
                                         type = z.name,
                                         ifshow = x.ifshow
                                     }
                              ).FirstOrDefaultAsync();
                return results ?? new DeviceDto();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                return new DeviceDto();
            }
        }

        public async Task<List<ActiveTypeDto>> GetActiveTypesAsync()
        {
            try
            {
                using var Scope = _scopeFactory.CreateScope();
                var _context = Scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var results = await (_context.activeTypes.ToListAsync());
                var dto = results.Select(x => new ActiveTypeDto
                {
                    activeid = x.activeid,
                    name = x.name,
                }).ToList();
                return dto;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<ActiveTypeDto>();
            }
        }

        public async Task<List<SensorTypeDto>> GetSensorTypesAsync()
        {
            try
            {
                using var Scope = _scopeFactory.CreateScope();
                var _context = Scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var results = await (_context.sensorTypes.ToListAsync());
                var dto = results.Select(x => new SensorTypeDto
                {
                    TypeId = x.typeid,
                    Name = x.name,
                }).ToList();
                return dto;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<SensorTypeDto>();
            }
        }
        public async Task<List<CodeTypeDto>> GetCodeTypeAsync()
        {
            try
            {
                using var Scope = _scopeFactory.CreateScope();
                var _context = Scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var results = await (_context.codetypes.ToListAsync());
                var dto = results.Select(x => new CodeTypeDto
                {
                    Id = x.id,
                    CodeTypeId = x.CodetypeId,
                    NameCodeType = x.Name,
                }).ToList();
                return dto;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<CodeTypeDto>();
            }
        }

        public async Task<List<ControlcodeDto>> GetListControlcode()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var results = from x in _context.controlcodes
                              join y in _context.devices on x.devid equals y.devid
                              join z in _context.codetypes on x.codetypeid equals z.CodetypeId
                              join g in _context.sensorTypes on x.typeid equals g.typeid
                              join h in _context.activeTypes on x.activeid equals h.activeid
                              where (x.ifshow == 1)
                              select new ControlcodeDto
                              {
                                  CodeId = x.codeid,
                                  Devid = x.devid,
                                  Code = x.code,
                                  CodeType = z.Name,
                                  DeviceName = y.name,
                                  NameControlcode = x.name,
                                  Active = h.name,
                                  Factor = x.factor,
                                  SensorType = g.name,
                                  High = x.high,
                                  Low = x.low
                              };
                return await results.ToListAsync();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                return new List<ControlcodeDto>();
            }
        }

        public async Task<int> CreateControlcodeAsync(CreateControlcodeDto dto)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                bool check = _context.controlcodes.Any(x => x.codeid == dto.CodeId);
                if (check)
                {
                    Console.WriteLine("Controlcode exits");
                    return 0;
                }
                var data = new Controlcode
                {
                    codeid = dto.CodeId,
                    devid = _context.devices.Where(x => x.name == dto.DeviceName).Select(x => x.devid).FirstOrDefault(),
                    code = dto.Code,
                    activeid = _context.activeTypes.Where(x => x.name == dto.Active).Select(x => x.activeid).FirstOrDefault(),
                    codetypeid = _context.codetypes.Where(x => x.Name == dto.CodeType).Select(x => x.CodetypeId).FirstOrDefault(),
                    name = dto.NameControlcode,
                    factor = dto.Factor,
                    typeid = _context.sensorTypes.Where(x => x.name == dto.SensorType).Select(x => x.typeid).FirstOrDefault(),
                    high = dto.High,
                    low = dto.Low,
                    ifshow = 1,
                    ifcal = 1
                };
                await _context.controlcodes.AddAsync(data);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {

                return 0;
            }
        }

        public async Task<int> EditControlcodeAsync(EditControlcodeDto dto)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var find = _context.controlcodes.FirstOrDefault(x => x.codeid == dto.CodeId);
                if (find == null)
                {
                    return 0;
                }
                find.name = dto.NameControlcode;
                find.typeid = _context.sensorTypes.Where(x => x.name == dto.SensorType).Select(x => x.typeid).FirstOrDefault();
                find.activeid = _context.activeTypes.Where(x => x.name == dto.Active).Select(x => x.activeid).FirstOrDefault();
                find.code = dto.Code;
                find.codetypeid = _context.codetypes.Where(x => x.Name == dto.CodeType).Select(x => x.CodetypeId).FirstOrDefault();
                find.devid = _context.devices.Where(x => x.name == dto.DeviceName).Select(x => x.devid).FirstOrDefault();
                find.factor = dto.Factor;
                find.high = dto.High;
                find.low = dto.Low;
                find.ifshow = 1;
                find.ifcal = 1;
                _context.controlcodes.Update(find);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<int> DeleteControlcodeAsync(int codeid)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var finDevice = _context.controlcodes.FirstOrDefault(x => x.codeid == codeid);
                if (finDevice == null)
                {
                    return 0;
                }

                finDevice.ifshow = 0;

                _context.controlcodes.Update(finDevice);
                await _context.SaveChangesAsync();
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<SensorTypeDto> GetSensorTypeByIdAsync(int id)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var find = await _context.sensorTypes.FirstOrDefaultAsync(x => x.typeid == id);
                var result = new SensorTypeDto
                {
                    Name = find.name,
                    TypeId = find.typeid,
                };
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new();
            }
        }

        public async Task<List<ControlcodeDto>> GetControlcodeByDevidAsync(int id)
        {
            // Sử dụng List<ControlcodeDto> rỗng thay vì new() để rõ ràng hơn
            var resultList = new List<ControlcodeDto>();

            try
            {
                // 1. Khởi tạo scope và context như cũ
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();

                // 2. Tối ưu hóa truy vấn bằng cách sử dụng JOINs/Include
                // Dùng LINQ Join hoặc Eager Loading (Include) để lấy dữ liệu từ các bảng liên quan 
                // trong MỘT truy vấn duy nhất.

                var query = from cc in _context.controlcodes
                                // Điều kiện lọc: Lấy tất cả Controlcodes có devid = id
                            where cc.devid == id && cc.ifshow == 1

                            // LEFT JOIN các bảng liên quan để lấy tên (Name)
                            join d in _context.devices on cc.devid equals d.devid into deviceGroup
                            from device in deviceGroup.DefaultIfEmpty() // LEFT JOIN

                            join at in _context.activeTypes on cc.activeid equals at.activeid into activeGroup
                            from activeType in activeGroup.DefaultIfEmpty()

                            join ct in _context.codetypes on cc.codetypeid equals ct.CodetypeId into codetypeGroup
                            from codeType in codetypeGroup.DefaultIfEmpty()

                            join st in _context.sensorTypes on cc.typeid equals st.typeid into sensorTypeGroup
                            from sensorType in sensorTypeGroup.DefaultIfEmpty()

                                // 3. Project kết quả sang ControlcodeDto
                            select new ControlcodeDto
                            {
                                CodeId = cc.codeid,
                                Devid = cc.devid,
                                NameControlcode = cc.name,
                                Code = cc.code,
                                Factor = cc.factor,
                                High = cc.high,
                                Low = cc.low,

                                // Lấy các trường tên từ các bảng đã join
                                DeviceName = device.name,
                                Active = activeType.name,
                                CodeType = codeType.Name,
                                SensorType = sensorType.name
                            };

                // 4. Thực thi truy vấn và trả về List
                resultList = await query.ToListAsync();

                return resultList;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi ra console
                Console.WriteLine($"Lỗi khi lấy danh sách Controlcode: {ex.Message}");
                // Trả về danh sách rỗng nếu có lỗi xảy ra
                return resultList;
            }
        }

        public async Task<bool> AddCodeTypeAsync(CodeTypeDto dto)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var data = new Codetype
                {
                    CodetypeId = dto.CodeTypeId,
                    Name = dto.NameCodeType
                };
                await _context.codetypes.AddAsync(data);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> UpdateCodeTypeAsync(CodeTypeDto dto)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var find = _context.codetypes.FirstOrDefault(x => x.id == dto.Id);
                if (find == null) return false;
                find.CodetypeId = dto.CodeTypeId;
                find.Name = dto.NameCodeType;
                _context.codetypes.Update(find);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteCodeTypeAsync(int id)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var find = _context.codetypes.FirstOrDefault(x => x.id == id);
                if (find == null) return false;

                _context.codetypes.Remove(find);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> AddSensorTypeAsync(SensorTypeDto dto)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var data = new SensorType
                {
                    typeid = dto.TypeId,
                    name = dto.Name,
                };
                await _context.sensorTypes.AddAsync(data);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> UpdateSensorTypeAsync(SensorTypeDto dto)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var find = _context.sensorTypes.FirstOrDefault(x => x.typeid == dto.TypeId);
                if (find == null) return false;
                find.name = dto.Name;
                _context.sensorTypes.Update(find);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteSensorTypeAsync(int typeId)
        {
            try
            {
                var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<PowerTempWatchContext>();
                var find = _context.sensorTypes.FirstOrDefault(x => x.typeid == typeId);
                if (find == null) return false;

                _context.sensorTypes.Remove(find);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                return false;
            }
        }


    }
}
