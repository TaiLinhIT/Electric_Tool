using Electric_Meter.Dto;
using Electric_Meter.Dto.ActiveTypeDto;
using Electric_Meter.Dto.CodeTypeDto;
using Electric_Meter.Dto.ControlcodeDto;
using Electric_Meter.Dto.DeviceDto;
using Electric_Meter.Dto.SensorDataDto;
using Electric_Meter.Dto.SensorTypeDto;
using Electric_Meter.Models;

namespace Electric_Meter.Interfaces
{
    public interface IService
    {
        SystemParameter LoadSystemParameters();
        void SaveSystemParameters(SystemParameter parameters);
        bool TestConnection(SystemParameter parameters);
        byte[] ConvertHexStringToByteArray(string requestHex);
        string ConvertToHex(int address);
        Task<SensorTypeDto> GetSensorTypeByIdAsync(int id);
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
        Task<List<ControlcodeDto>> GetControlcodeByDevidAsync(int devid);
        Task<bool> InsertToSensorDataAsync(SensorDataDto dto);
        List<DeviceVM> GetDevicesList();

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
