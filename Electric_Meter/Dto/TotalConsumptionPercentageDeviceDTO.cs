using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Electric_Meter.Dto
{
    public class TotalConsumptionPercentageDeviceDTO
    {
        public int devid { get; set; }
        public string DeviceName { get; set; }
        public double TotalConsumption { get; set; }
        public double Percentage { get; set; }
    }
}
