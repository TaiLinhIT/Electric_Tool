using Electric_Meter.Models;

namespace Electric_Meter.Interfaces
{
    public interface IService
    {
        Task<int> InsertToDevice(Device machine);
        //Task<List<DvElectricDataTemp>> GetListDataAsync(int address);
        Task<int> EditToDevice(Device machine);
        Task<int> DeleteToDevice(Device machine);
        //Task InsertToElectricDataTempAsync(DvElectricDataTemp dvElectricDataTemp);
        Task<bool> InsertToSensorDataAsync(SensorData data);
        List<Device> GetDevicesList();
        List<Device> GetDevicesByAssembling(string key);
        Task<List<Device>> GetActiveDevicesAsync();
        Task<List<Device>> GetDeviceByIdAsync(int devid);
        Task<List<SensorData>> GetLatestSensorByDeviceAsync(int devid);
    }
}
