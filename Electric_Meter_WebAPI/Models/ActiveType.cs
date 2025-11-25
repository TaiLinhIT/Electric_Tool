
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Electric_Meter_WebAPI.Models
{
    [Table("ActiveType")]
    public class ActiveType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int activeid { get; set; }
        public string name { get; set; }
    }
}
