using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Electric_Meter.Models;

[Table("dv_ElectricDataTemp")]
public partial class DvElectricDataTemp
{
    [Key]
    public int Id { get; set; }

    public int IdMachine { get; set; }

    public double? Ia { get; set; }

    public double? Ib { get; set; }

    public double? Ic { get; set; }

    public double? Pt { get; set; }

    public double? Pa { get; set; }

    public double? Pb { get; set; }

    public double? Pc { get; set; }

    public double? Ua { get; set; }

    public double? Ub { get; set; }

    public double? Uc { get; set; }

    public double? Exp { get; set; }

    public double? Imp { get; set; }

    public double? TotalElectric { get; set; }
    public DateTime? UploadDate { get; set; }

}
