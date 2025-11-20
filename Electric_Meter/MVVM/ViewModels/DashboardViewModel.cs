using System.Windows;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using Electric_Meter.Dto;
using Electric_Meter.Services;

using LiveCharts;
using LiveCharts.Defaults; // Needed for ObservableValue
using LiveCharts.Wpf;



namespace Electric_Meter.MVVM.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        #region [ Fields - Private Dependencies ]
        private readonly Service _services;
        #endregion
        #region [ Observable Properties ]
        [ObservableProperty] private SeriesCollection overviewSeriesCollection;
        [ObservableProperty] private string[] _overviewLabels;
        [ObservableProperty] private SeriesCollection _weightSeriesCollection;
        [ObservableProperty] private string[] _weightLabels;
        [ObservableProperty] private SeriesCollection _caloriesSeriesCollection;
        [ObservableProperty] private SeriesCollection _activityDurationSeriesCollection;
        [ObservableProperty] private string[] _activityDurationLabels;
        #endregion
        #region [ constructor ]
        public DashboardViewModel(Service services)
        {
            _services = services;
            InitializeOverviewChart();
            InitializeWeightChart();
            InitializeCaloriesChart();
            InitializeActivityDurationChart();
        }

        #endregion





        public Func<double, string> OverviewFormatter { get; set; }



        public Func<double, string> WeightFormatter { get; set; }


        public Func<ChartPoint, string> CaloriesPointLabel { get; set; }

        #region [ function ]
        private async Task InitializeOverviewChart()
        {
            List<LatestSensorByDeviceYear> lst = await _services.GetLatestSensorByDeviceYear(2025);
            OverviewSeriesCollection = new SeriesCollection
            {
                new RowSeries
                {
                    Title = "Series 1",
                    Values = new ChartValues<int> { 4, 4, 7, 2, 8 },
                    StrokeThickness = 4,

                    Stroke = new SolidColorBrush(Colors.Blue),
                    Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 255)),
                }
            };

            // Cập nhật thuộc tính OverviewLabels
            // Dữ liệu mẫu chỉ có 5 điểm, nên Labels nên được rút gọn:
            OverviewLabels = new[] { "P1", "P2", "P3", "P4", "P5" };

            // Nếu bạn muốn giữ 12 tháng:
            OverviewLabels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

            OverviewFormatter = value => value.ToString("N0");
        }

        private void InitializeWeightChart()
        {
            WeightSeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Weight",
                    Values = new ChartValues<ObservableValue> // Using ObservableValue for potential live updates
                    {
                        new ObservableValue(75.2), // Day 1
                        new ObservableValue(75.0),
                        new ObservableValue(74.8),
                        new ObservableValue(74.9),
                        new ObservableValue(74.5),
                        new ObservableValue(74.6),
                        new ObservableValue(74.2)  // Day 7
                    },
                    PointGeometrySize = 8,
                    Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue line
                    Fill = new LinearGradientBrush
                    {
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(30, 33, 150, 243), 0),
                            new GradientStop(Colors.Transparent, 1)
                        },
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1)
                    }
                }
            };
            WeightLabels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            WeightFormatter = value => value.ToString("N1");
        }

        private void InitializeCaloriesChart()
        {
            CaloriesPointLabel = chartPoint =>
                string.Format("{0} ({1:P1})", chartPoint.SeriesView.Title, chartPoint.Participation);

            CaloriesSeriesCollection = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "Jogging",
                    Values = new ChartValues<double> { 1200 },
                    DataLabels = true,
                    LabelPoint = CaloriesPointLabel,
                    Fill = new SolidColorBrush(Color.FromRgb(106, 27, 154)) // Purple
                },
                new PieSeries
                {
                    Title = "Cycling",
                    Values = new ChartValues<double> { 800 },
                    DataLabels = true,
                    LabelPoint = CaloriesPointLabel,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 111, 97)) // Orange/Red
                },
                new PieSeries
                {
                    Title = "Walking",
                    Values = new ChartValues<double> { 500 },
                    DataLabels = true,
                    LabelPoint = CaloriesPointLabel,
                    Fill = new SolidColorBrush(Color.FromRgb(50, 205, 50)) // Green
                },
                new PieSeries
                {
                    Title = "Other",
                    Values = new ChartValues<double> { 300 },
                    DataLabels = true,
                    LabelPoint = CaloriesPointLabel,
                    Fill = new SolidColorBrush(Color.FromRgb(128, 128, 128)) // Grey
                }
            };
        }

        private void InitializeActivityDurationChart()
        {
            ActivityDurationSeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Duration",
                    Values = new ChartValues<double> { 60, 45, 30, 90 }, // Example durations for Jogging, Cycling, Walking, Gym
                    Fill = new SolidColorBrush(Color.FromRgb(60, 179, 113)) // Medium Sea Green
                }
            };
            ActivityDurationLabels = new[] { "Jogging", "Cycling", "Walking", "Gym" };
        }

        #endregion
    }
}
