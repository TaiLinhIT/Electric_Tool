namespace Electric_Meter.Dto.DeviceDto
{
    public class CreateDeviceDto
    {
        public int devid { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string active { get; set; }
        public int ifshow { get; set; }
    }
}
