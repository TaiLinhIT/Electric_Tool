
using Electric_Meter_WebAPI.Dto;
using Electric_Meter_WebAPI.Dto.DeviceDto;
using Electric_Meter_WebAPI.Models;

namespace Electric_Meter_WebAPI.Interfaces
{
    public interface IService
    {
        Task<List<DeviceDto>> GetListDevice();
        Task<int> InsertToDevice(Device machine);
        //Task<List<DvElectricDataTemp>> GetListDataAsync(int address);
        Task<int> EditToDevice(Device machine);
        Task<int> DeleteToDevice(Device machine);
        //Task InsertToElectricDataTempAsync(DvElectricDataTemp dvElectricDataTemp);
        Task<bool> InsertToSensorDataAsync(SensorData data);
        List<Device> GetDevicesByAssembling(string key);
        Task<List<Device>> GetActiveDevicesAsync();
        Task<List<Device>> GetDeviceByIdAsync(int devid);
        Task<List<SensorData>> GetLatestSensorByDeviceAsync(int devid);
        Task<List<DailyConsumptionDTO>> GetDailyConsumptionDTOs(int devid);
        Task<List<LatestSensorByDeviceYear>> GetLatestSensorByDeviceYear(int year);
        Task<List<TotalConsumptionPercentageDeviceDTO>> GetRatioMonthlyDevice(int month, int year);
    }
}
