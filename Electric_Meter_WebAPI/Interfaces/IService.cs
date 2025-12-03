
using Electric_Meter_WebAPI.Dto;
using Electric_Meter_WebAPI.Dto.ActiveTypeDto;
using Electric_Meter_WebAPI.Dto.CodeTypeDto;
using Electric_Meter_WebAPI.Dto.ControlcodeDto;
using Electric_Meter_WebAPI.Dto.DeviceDto;
using Electric_Meter_WebAPI.Dto.SensorTypeDto;
using Electric_Meter_WebAPI.Models;

namespace Electric_Meter_WebAPI.Interfaces
{
    public interface IService
    {
        Task<List<CodeTypeDto>> GetCodeTypeAsync();
        Task<List<ActiveTypeDto>> GetActiveTypesAsync();
        Task<List<SensorTypeDto>> GetSensorTypesAsync();
        Task<DeviceDto> GetDeviceByDevid(int devid);
        Task<List<DeviceDto>> GetListDevice();
        Task<int> InsertToDevice(CreateDeviceDto dto);
        Task<int> EditDeviceAsync(EditDeviceDto dto);
        Task<int> DeleteDeviceAsync(int devid);
        Task<List<ControlcodeDto>> GetListControlcode();
        Task<int> CreateControlcodeAsync(CreateControlcodeDto dto);
        Task<int> EditControlcodeAsync(EditControlcodeDto dto);
        Task<int> DeleteControlcodeAsync(int devid);
        Task<bool> InsertToSensorDataAsync(SensorData data);
        List<Device> GetDevicesByAssembling(string key);
        Task<List<Device>> GetDeviceByIdAsync(int devid);
        Task<List<SensorData>> GetLatestSensorByDeviceAsync(int devid);
        Task<List<DailyConsumptionDTO>> GetDailyConsumptionDTOs(int devid);
        Task<List<LatestSensorByDeviceYear>> GetLatestSensorByDeviceYear(int year);
        Task<List<TotalConsumptionPercentageDeviceDTO>> GetRatioMonthlyDevice(int month, int year);
    }
}
