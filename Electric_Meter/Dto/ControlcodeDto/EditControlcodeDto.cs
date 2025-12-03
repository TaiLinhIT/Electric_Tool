using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Electric_Meter.Dto.ControlcodeDto
{
    public class EditControlcodeDto
    {
        public int CodeId { get; set; }

        public string DeviceName { get; set; }
        public string Active { get; set; }
        public string Code { get; set; }
        public string CodeType { get; set; }// read or write
        public string NameControlcode { get; set; }
        public double Factor { get; set; }
        public string SensorType { get; set; }// temperature or electronic
        public decimal? High { get; set; }
        public decimal? Low { get; set; }
    }
}
