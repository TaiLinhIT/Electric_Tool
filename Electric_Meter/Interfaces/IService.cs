using Electric_Meter.Dto;
using Electric_Meter.Dto.ActiveTypeDto;
using Electric_Meter.Dto.CodeTypeDto;
using Electric_Meter.Dto.ControlcodeDto;
using Electric_Meter.Dto.DeviceDto;
using Electric_Meter.Dto.SensorTypeDto;
using Electric_Meter.Models;

namespace Electric_Meter.Interfaces
{
    public interface IService
    {
        Task<List<CodeTypeDto>> GetCodeTypeAsync();
        Task<List<ActiveTypeDto>> GetActiveTypesAsync();
        Task<List<SensorTypeDto>> GetSensorTypesAsync();
        Task<List<DeviceDto>> GetListDeviceAsync();
        Task<DeviceDto> GetDeviceByDevidAsync(int devid);
        Task<bool> CreateDeviceAsync(CreateDeviceDto dto);
        Task<bool> UpdateDeviceAsync(EditDeviceDto dto);
        Task<bool> DeleteDeviceAsync(int devid);
        Task<bool> CreateControlcodeAsync(CreateControlcodeDto dto);
        Task<bool> UpdateControlcodeAsync(EditControlcodeDto dto);
        Task<bool> DeleteControlcodeAsync(int codeid);
        Task<List<ControlcodeDto>> GetListControlcodeAsync();

        Task<bool> InsertToSensorDataAsync(SensorData data);
        List<DeviceVM> GetDevicesList();
        List<ControlcodeVM> GetControlCodeListByDevid(int devid);
        //List<Device> GetDevicesByAssembling(string key);
        Task<List<Device>> GetActiveDevicesAsync();
        Task<List<Device>> GetDeviceByIdAsync(int devid);
        Task<List<SensorData>> GetLatestSensorByDeviceAsync(int devid);
        Task<List<DailyConsumptionDTO>> GetDailyConsumptionDTOs(int devid);
        Task<List<LatestSensorByDeviceYear>> GetLatestSensorByDeviceYear(int year);
        Task<List<TotalConsumptionPercentageDeviceDTO>> GetRatioMonthlyDevice(int month, int year);
    }
}
