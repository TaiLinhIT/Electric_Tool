using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Electric_Meter.Models
{
    [Table("dv_Machine")]
    public class Machine
    {
        public int Id { get; set; }
        public string Port { get; set; }
        public string Name { get; set; }
        public string Line { get; set; }
        public int Baudrate { get; set; }
        public int Address { get; set; }
        public string LineCode { get; set; }
    }
}
