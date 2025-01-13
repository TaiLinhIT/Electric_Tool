using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Electric_Meter.Models
{
    public class DeviceConfig
    {
        public string Port { get; set; } = string.Empty;
        public int Baudrate { get; set; } = 115200;
        public string Factory { get; set; } = string.Empty;

        public int AddressMachine { get; set; }//sửa thành address
    }
}
