namespace Electric_Meter_WebAPI.Dto
{
    public class TotalConsumptionPercentageDeviceDTO
    {
        public int devid { get; set; }
        public string DeviceName { get; set; }
        public double TotalConsumption { get; set; }
        public double Percentage { get; set; }
    }
}
