namespace Electric_Meter_WebAPI.Dto.SensorDataDto
{
    public class SensorDataDto
    {
        public int Devid { get; set; }
        public int Codeid { get; set; }
        public double Value { get; set; }
        public DateTime Day { get; set; }
    }
}
