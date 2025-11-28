using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Electric_Meter.Models
{
    [Table("ActiveType")]
    public class ActiveType
    {
        [Key]
        public int activeid { get; set; }
        public string name { get; set; }
    }
}
