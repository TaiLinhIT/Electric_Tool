using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Electric_Meter_WebAPI.Models
{
    [Table("SensorType")]
    public class SensorType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int typeid { get; set; }
        public string name { get; set; }
    }
}
