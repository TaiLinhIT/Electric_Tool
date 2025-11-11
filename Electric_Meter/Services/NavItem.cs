using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using Wpf.Ui.Controls;

namespace Electric_Meter.Services
{
    public class NavItem : ObservableObject
    {
        public Type TargetPageType { get; set; }

        private string _content;
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public SymbolIcon Icon { get; set; }
    }

}
