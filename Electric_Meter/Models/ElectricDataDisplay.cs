using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Electric_Meter.Models
{
    public class ElectricDataDisplay : ObservableObject
    {
        private double _value;
        public string Name { get; set; }
        public string Unit { get; set; }
        public double Value
        {
            get => _value;
            set => SetProperty(ref _value, value); // Sử dụng SetProperty để thông báo thay đổi
        }
    }
}
