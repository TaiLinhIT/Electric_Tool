using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Electric_Meter_WebAPI.Models
{ 

    [Table("Codetype")]
    public class Codetype
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        [Column("codetypeid")]
        public string CodetypeId { get; set; }
        [Column("name")]
        public string Name { get; set; }
    }
}
