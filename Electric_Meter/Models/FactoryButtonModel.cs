using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Electric_Meter.Models
{
    public class FactoryButtonModel
    {
        public string Name { get; set; } // Tên Factory (button chính)
        public string AssemblingName { get; set; } // Tên Assembling (button phụ)

        public ICommand MainCommand { get; set; } // Command cho nút chính
        public ICommand AssemblingCommand { get; set; } // Command cho nút phụ
    }
}
