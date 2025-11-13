using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Electric_Meter.MVVM.ViewModels;

namespace Electric_Meter.MVVM.Views
{
    /// <summary>
    /// Interaction logic for ToolView.xaml
    /// </summary>
    public partial class ToolView : UserControl
    {
        public ToolView(ToolViewModel toolViewModel)
        {
            InitializeComponent();
            DataContext = toolViewModel;
        }
    }
}
