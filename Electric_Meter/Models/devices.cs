using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Electric_Meter.Models
{
    [Table("devices")]
    public class Device
    {
        [Key]
        public int devid { get; set; }
        public int address { get; set; }
        public string name { get; set; }
        public string port { get; set; }
        public int baudrate { get; set; }
        public int activeid { get; set; }
        public int typeid { get; set; }
        public int ifshow { get; set; }
    }
}
