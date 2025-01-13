using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Electric_Meter.Models
{
    [Table("dv_FactoryAddress_Configs")]
    public partial class DvFactoryAssembling
    {
        public int Id { get; set; }
        public string? Factory { get; set; }
        public string? Assembling { get; set; }
    }
}
