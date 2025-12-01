using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Electric_Meter.Models
{
    [Table("devices")]
    public class Device
    {
        [Key]
        public int devid { get; set; }
        public string name { get; set; }
        public int activeid { get; set; }
        public int typeid { get; set; }
        public int ifshow { get; set; }
    }
}
