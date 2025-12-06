using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Electric_Meter.Models
{
    public class SystemParameter
    {
        public string BackupDbLocation { get; set; }
        public string DatabaseName { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
    }
}
