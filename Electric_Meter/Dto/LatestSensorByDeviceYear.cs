using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Electric_Meter.Dto
{
    public class LatestSensorByDeviceYear
    {
        public int devid { get; set; }
        public string deviceName { get; set; }
        public double value { get; set; }
    }
}
