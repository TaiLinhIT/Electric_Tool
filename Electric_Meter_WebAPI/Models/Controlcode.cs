using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Electric_Meter_WebAPI.Models
{
    [Table("controlcode")]
    public class Controlcode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int codeid { get; set; }
        public int devid { get; set; }
        public string code { get; set; }
        public int activeid { get; set; }
        public string codetypeid { get; set; }
        public string name { get; set; }
        public double factor { get; set; }
        public int? typeid { get; set; }

        public decimal? high { get; set; }

        public decimal? low { get; set; }

        public int? ifshow { get; set; }

        public int? ifcal { get; set; }
    }
}
