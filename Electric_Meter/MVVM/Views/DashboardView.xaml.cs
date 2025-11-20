using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Electric_Meter.MVVM.ViewModels;

using LiveCharts;

using LiveCharts.Wpf;

namespace Electric_Meter.MVVM.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView(DashboardViewModel dashboardViewModel)
        {
            InitializeComponent();
            DataContext = dashboardViewModel;
        }
        // Event handler for the PieChart_OnDataClick
        private void PieChart_OnDataClick(object sender, ChartPoint chartPoint)
        {
            // The sender is the PieSlice visual element.
            // We need to find its parent PieChart.
            var pieSlice = sender as LiveCharts.Wpf.Points.PieSlice;
            if (pieSlice == null)
            {
                // Fallback: If for some reason sender isn't a PieSlice, try direct cast (less likely with this event)
                var directChart = sender as LiveCharts.Wpf.PieChart;
                if (directChart != null)
                {
                    HandlePieChartClick(directChart, chartPoint);
                }
                System.Diagnostics.Debug.WriteLine("Warning: sender was not a PieSlice or PieChart directly.");
                return;
            }

            // Traverse the visual tree upwards to find the PieChart parent
            LiveCharts.Wpf.PieChart chart = null;
            DependencyObject current = pieSlice;
            while (current != null)
            {
                chart = current as LiveCharts.Wpf.PieChart;
                if (chart != null)
                {
                    break; // Found the PieChart!
                }
                current = VisualTreeHelper.GetParent(current);
            }

            if (chart != null)
            {
                HandlePieChartClick(chart, chartPoint);
            }
            else
            {
                // Handle the case where the PieChart parent wasn't found (shouldn't happen if setup correctly)
                System.Diagnostics.Debug.WriteLine("Error: Could not find the parent PieChart for the clicked slice.");
            }
        }

        // Helper method to encapsulate the chart manipulation logic
        private void HandlePieChartClick(LiveCharts.Wpf.PieChart chart, ChartPoint chartPoint)
        {
            // Reset push-out for all slices
            foreach (PieSeries series in chart.Series)
            {
                series.PushOut = 0; // Reset all other slices
            }

            // "Explode" the clicked slice
            var selectedSeries = (PieSeries)chartPoint.SeriesView; // chartPoint.SeriesView gives the specific series
            selectedSeries.PushOut = 15; // Amount to "explode" the slice
        }


    }
}
