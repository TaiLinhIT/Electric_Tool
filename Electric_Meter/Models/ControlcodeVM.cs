using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Electric_Meter.Models
{
    public class ControlcodeVM
    {
        public int codeid { get; set; }
        public int devid { get; set; }
        public string deviceName { get; set; }
        public string code { get; set; }
        public string active { get; set; }
        public string codetype { get; set; }// read or write
        public string name { get; set; }
        public double factor { get; set; }
        public string type { get; set; } // electronic or temperature

        public decimal? high { get; set; }

        public decimal? low { get; set; }

        public int? ifshow { get; set; }

        public int? ifcal { get; set; }
    }
}
