
using Electric_Meter_WebAPI.Dto;
using Electric_Meter_WebAPI.Dto.ActiveTypeDto;
using Electric_Meter_WebAPI.Dto.CodeTypeDto;
using Electric_Meter_WebAPI.Dto.ControlcodeDto;
using Electric_Meter_WebAPI.Dto.DeviceDto;
using Electric_Meter_WebAPI.Dto.SensorDataDto;
using Electric_Meter_WebAPI.Dto.SensorTypeDto;
using Electric_Meter_WebAPI.Models;

namespace Electric_Meter_WebAPI.Interfaces
{
    public interface IService
    {
        Task<List<ControlcodeDto>> GetControlcodeByDevidAsync(int id);
        Task<SensorTypeDto> GetSensorTypeByIdAsync(int id);
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
        Task<bool> InsertToSensorDataAsync(SensorDataDto dto);
        List<Device> GetDevicesByAssembling(string key);
        Task<List<Device>> GetDeviceByIdAsync(int devid);
        Task<List<SensorData>> GetLatestSensorByDeviceAsync(int devid);
        Task<List<DailyConsumptionDTO>> GetDailyConsumptionDTOs(int devid);
        Task<List<LatestSensorByDeviceYear>> GetLatestSensorByDeviceYear(int year);
        Task<List<TotalConsumptionPercentageDeviceDTO>> GetRatioMonthlyDevice(int month, int year);
        Task<bool> AddCodeTypeAsync(CodeTypeDto dto);
        Task<bool> UpdateCodeTypeAsync(CodeTypeDto dto);
        Task<bool> DeleteCodeTypeAsync(int id);
        Task<bool> AddSensorTypeAsync(SensorTypeDto dto);
        Task<bool> UpdateSensorTypeAsync(SensorTypeDto dto);
        Task<bool> DeleteSensorTypeAsync(int typeId);
    }
}
