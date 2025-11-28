using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Electric_Meter.Models
{
    public class DeviceVM
    {
        public int devid { get; set; }
        public int address { get; set; }
        public string name { get; set; }
        public string port { get; set; }
        public string assembling { get; set; }
        public int baudrate { get; set; }
        public string active { get; set; }
        public string type { get; set; }
        public int ifshow { get; set; }
    }
}
