namespace Electric_Meter_WebAPI.Dto
{
    public class LatestSensorByDeviceYear
    {
        public int devid { get; set; }
        public string device_name { get; set; }
        public double TotalValue { get; set; }
    }
}
