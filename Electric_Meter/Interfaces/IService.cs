using Electric_Meter.Dto;
using Electric_Meter.Models;

namespace Electric_Meter.Interfaces
{
    public interface IService
    {
        Task<int> InsertToDevice(Device machine);
        Task<int> EditToDevice(Device machine);
        Task<int> DeleteToDevice(Device machine);
        Task<int> InsertToControlcode(Controlcode code);
        Task<int> EditToControlcode(Controlcode code);
        Task<int> DeleteToControlcode(Controlcode code);
        Task<bool> InsertToSensorDataAsync(SensorData data);
        List<DeviceVM> GetDevicesList();
        List<ControlcodeVM> GetControlCodeListByDevid(int devid);
        List<Device> GetDevicesByAssembling(string key);
        Task<List<Device>> GetActiveDevicesAsync();
        Task<List<Device>> GetDeviceByIdAsync(int devid);
        Task<List<SensorData>> GetLatestSensorByDeviceAsync(int devid);
        Task<List<DailyConsumptionDTO>> GetDailyConsumptionDTOs(int devid);
        Task<List<LatestSensorByDeviceYear>> GetLatestSensorByDeviceYear(int year);
        Task<List<TotalConsumptionPercentageDeviceDTO>> GetRatioMonthlyDevice(int month, int year);
    }
}
