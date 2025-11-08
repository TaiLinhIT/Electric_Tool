using Electric_Meter.Models;

namespace Electric_Meter.Interfaces
{
    public interface IService
    {
        Task<int> InsertToMachine(Machine machine);
        Task<List<DvElectricDataTemp>> GetListDataAsync(int address);
        Task<int> EditToMachine(Machine machine);
        Task<int> DeleteToMachine(Machine machine);
        Task InsertToElectricDataTempAsync(DvElectricDataTemp dvElectricDataTemp);
        Task<bool> InsertToSensorDataAsync(SensorData data);
        List<Device> GetDevicesList();
    }
}
