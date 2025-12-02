using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Electric_Meter.Dto.DeviceDto
{
    public class EditDeviceDto
    {
        public int devid { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string active { get; set; }
        public int ifshow { get; set; }
    }
}
