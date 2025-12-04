using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using Electric_Meter.Dto;
using Electric_Meter.Interfaces;
using Electric_Meter.Models;
using Electric_Meter.Services;

using LiveCharts;
using LiveCharts.Wpf;



namespace Electric_Meter.MVVM.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        #region [ Fields - Private Dependencies ]
        private readonly IService _services;
        private readonly LanguageService _languageService;
        private readonly DispatcherTimer _updateTimer;
        #endregion
        #region [ Observable Properties ]
        [ObservableProperty] private List<int> lstYearBarChart;
        [ObservableProperty] private int selectedYearBarChart;
        [ObservableProperty] private List<int> lstMonthColumnChart;
        [ObservableProperty] private int selectedMonthColumnChart;
        [ObservableProperty] private List<int> lstMonthPieChart;
        [ObservableProperty] private int selectedMonthPieChart;
        [ObservableProperty] private List<int> lstYearPieChart;
        [ObservableProperty] private int selectedYearPieChart;
        [ObservableProperty] private List<int> lstYearColumnChart;
        [ObservableProperty] private int selectedYearColumnChart;
        [ObservableProperty] private ObservableCollection<DeviceVM> lstDeviceChartLine;
        [ObservableProperty] private DeviceVM selectedDeviceChartLine;
        [ObservableProperty] private SeriesCollection chartBarSeriesCollection;
        [ObservableProperty] private string[] lstChartBarLabels;
        [ObservableProperty] private SeriesCollection chartLineSeriesCollection;
        [ObservableProperty] private string[] lstChartLineLabels;
        [ObservableProperty] private SeriesCollection chartPieSeriesCollection;
        [ObservableProperty] private SeriesCollection chartColumnSeriesCollection;
        [ObservableProperty] private string[] lstChartColumnLabels;
        #endregion
        #region [ constructor ]
        public DashboardViewModel(IService services, LanguageService languageService)
        {
            _services = services;
            _languageService = languageService;
            LoadYearBarChart();
            LoadMonthColumnChart();
            LoadMonthPieChart();
            LoadYearPieChart();
            LoadYearColumnChart();
            LoadDeviceChartLine();
            _languageService = languageService;
            _languageService.LanguageChanged += UpdateTexts;
            UpdateTexts();
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(50); // Cập nhật mỗi 5 phút
            _updateTimer.Tick += UpdateChartsOnTimerTick;
            _updateTimer.Start();
        }

        #endregion


        partial void OnSelectedYearBarChartChanged(int oldValue, int newValue)
        {

            // Kiểm tra an toàn trước khi gọi
            if (newValue != 0)
            {
                // Gọi hàm Wrapper với giá trị năm mới
                InitializeChartBarAsyncWrapper(newValue);
            }
        }
        partial void OnSelectedYearColumnChartChanged(int oldValue, int newValue)
        {
            if (newValue != 0 && SelectedMonthColumnChart != 0)
            {
                InitializeChartColumnAsyncWrapper(SelectedMonthColumnChart, newValue);
            }
        }
        partial void OnSelectedMonthColumnChartChanged(int oldValue, int newValue)
        {
            if (newValue != 0 && SelectedYearColumnChart != 0)
            {
                InitializeChartColumnAsyncWrapper(newValue, SelectedYearColumnChart);
            }
        }
        partial void OnSelectedMonthPieChartChanged(int oldValue, int newValue)
        {
            if (newValue != 0 && SelectedYearPieChart != 0)
            {
                InitializeChartPieAsyncWrapper(newValue, SelectedYearPieChart);
            }
        }
        partial void OnSelectedYearPieChartChanged(int oldValue, int newValue)
        {
            if (newValue != 0 && SelectedMonthPieChart != 0)
            {
                InitializeChartPieAsyncWrapper(SelectedMonthPieChart, newValue);
            }
        }
        partial void OnSelectedDeviceChartLineChanged(DeviceVM oldValue, DeviceVM newValue)
        {
            if (newValue != null)
            {
                InitializeChartLineAsyncWrapper(newValue.devid);
            }
        }
        private async void InitializeChartBarAsyncWrapper(int year)
        {
            await InitializeChartBar(year);
        }
        private async void InitializeChartColumnAsyncWrapper(int month, int year)
        {
            await InitializeChartColumn(month, year);
        }
        private async void InitializeChartPieAsyncWrapper(int month, int year)
        {
            await InitializeChartPie(month, year);
        }
        private async void InitializeChartLineAsyncWrapper(int devid)
        {
            await InitializeChartLine(devid);
        }
        public Func<double, string> ChartBarFormatter { get; set; }



        public Func<double, string> ChartLineFormatter { get; set; }


        public Func<ChartPoint, string> CaloriesPointLabel { get; set; }

        #region [ function ]
        private async Task InitializeChartBar(int year)
        {
            try
            {
                List<LatestSensorByDeviceYear> lst = await _services.GetLatestSensorByDeviceYear(year);

                if (lst == null || !lst.Any())
                {
                    chartBarSeriesCollection = new SeriesCollection();
                    LstChartBarLabels = new string[] { "No Data" };
                    return;
                }

                var lables = new List<string>();
                var chartValues = new ChartValues<double>();

                foreach (var item in lst)
                {
                    chartValues.Add(item.TotalValue); // Thêm giá trị
                    lables.Add(item.device_name);    // Thêm tên thiết bị
                }
                Func<ChartPoint, string> LabelPointFormatter = chartPoint =>
                {
                    // Đối với RowSeries, giá trị nằm trên trục X
                    return chartPoint.X.ToString("N0") + " kWh";
                };
                ChartBarSeriesCollection = new SeriesCollection
                {
                    new RowSeries
                    {
                        Values = chartValues, // Gán tất cả giá trị vào Series này
                        Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Ví dụ: Màu xanh
                        DataLabels = true,
                        LabelPoint = LabelPointFormatter
                    }
                };

                // 3. Gán nhãn cho Trục Y
                LstChartBarLabels = lables.ToArray();
                ChartBarFormatter = value => value.ToString("N0");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Bar Chart: {ex.Message}");
                ChartBarSeriesCollection = new SeriesCollection();
                LstChartBarLabels = new string[] { "Error Loading Data" };
            }
        }

        private async Task InitializeChartLine(int devid)
        {
            try
            {
                List<DailyConsumptionDTO> lst = await _services.GetDailyConsumptionDTOs(devid);

                var chartValues = new ChartValues<double>();
                var labels = new List<string>();

                foreach (var item in lst)
                {

                    chartValues.Add(item.TotalDailyConsumption);

                    labels.Add(item.dayData.ToShortDateString());
                }
                Func<ChartPoint, string> LabelPointFormatter = chartPoint =>
                {
                    // Đối với RowSeries, giá trị nằm trên trục X
                    return chartPoint.X.ToString("N0") + " kWh";
                };
                ChartLineSeriesCollection = new SeriesCollection
                {
                     new LineSeries
                     {
                         Values = chartValues,
                         DataLabels = true,
                         PointGeometrySize = 8,
                         Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                     }
                };

                LstChartLineLabels = labels.ToArray(); // <-- Chứa ngày tháng

                ChartLineFormatter = value => value.ToString("N1");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task InitializeChartPie(int month, int year)
        {
            CaloriesPointLabel = chartPoint =>
            string.Format("{0} ({1:P1})", chartPoint.SeriesView.Title, chartPoint.Participation);
            try
            {

                List<TotalConsumptionPercentageDeviceDTO> lst = await _services.GetRatioMonthlyDevice(month, year);

                // Kiểm tra dữ liệu (đề phòng lỗi return null từ service)
                if (lst == null || !lst.Any())
                {
                    ChartPieSeriesCollection = new SeriesCollection(); // Khởi tạo Collection rỗng
                    return;
                }
                Func<ChartPoint, string> LabelPointFormatter = chartPoint =>
                {
                    // Đối với RowSeries, giá trị nằm trên trục X
                    return chartPoint.X.ToString("N0") + " kWh";
                };
                var seriesCollection = new SeriesCollection();
                var colors = new[]
                {
                    Color.FromRgb(106, 27, 154),   // Purple
                    Color.FromRgb(255, 111, 97),   // Orange/Red
                    Color.FromRgb(50, 205, 50),    // Green
                    Color.FromRgb(128, 128, 128)   // Grey
                    // Thêm màu sắc nếu số lượng thiết bị > 4
                };
                int colorIndex = 0;

                // LẶP QUA DỮ LIỆU VÀ TẠO PIESERIES TỪ PERCENTAGE
                foreach (var item in lst)
                {
                    seriesCollection.Add(new PieSeries
                    {
                        // Title sẽ là ID thiết bị (hoặc tên nếu DTO có)
                        Title = item.DeviceName,

                        // VALUES: Sử dụng Percentage (Tỷ lệ)
                        Values = new ChartValues<double> { item.Percentage },
                        DataLabels = true,
                        LabelPoint = CaloriesPointLabel, // Giữ nguyên formatter cũ
                        Fill = new SolidColorBrush(colors[colorIndex++ % colors.Length])
                    });
                }

                // Cập nhật thuộc tính của ViewModel
                ChartPieSeriesCollection = seriesCollection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Pie Chart: {ex.Message}");
            }
        }

        private async Task InitializeChartColumn(int month, int year)
        {
            try
            {
                List<TotalConsumptionPercentageDeviceDTO> lst = await _services.GetRatioMonthlyDevice(month, year);

                if (lst == null || !lst.Any())
                {
                    ChartColumnSeriesCollection = new SeriesCollection();
                    LstChartColumnLabels = new string[0];
                    return;
                }

                var chartValues = new ChartValues<double>();
                var labels = new List<string>();

                // LẶP QUA DỮ LIỆU VÀ TRÍCH XUẤT GIÁ TRỊ VÀ NHÃN
                foreach (var item in lst)
                {
                    // VALUES: Sử dụng TotalConsumption
                    chartValues.Add(item.TotalConsumption);

                    // LABELS: Sử dụng devid (hoặc tên thiết bị nếu DTO có)
                    labels.Add(item.DeviceName);
                }
                Func<ChartPoint, string> LabelPointFormatter = chartPoint =>
                {
                    // Đối với RowSeries, giá trị nằm trên trục X
                    return chartPoint.Y.ToString("N0") + " kWh";
                };
                // Cập nhật thuộc tính của ViewModel
                ChartColumnSeriesCollection = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Values = chartValues,
                        Fill = new SolidColorBrush(Color.FromRgb(60, 179, 113)),
                        DataLabels = true,
                        LabelPoint = LabelPointFormatter
                    }
                };

                // Cập nhật nhãn trục X
                LstChartColumnLabels = labels.ToArray(); // <-- Tên thiết bị
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Column Chart: {ex.Message}");
            }
        }


        #endregion


        #region [ Methods - Load Default ]
        public void LoadYearBarChart()
        {
            LstYearBarChart = new List<int>();
            int currentYear = DateTime.Now.Year;
            for (int year = currentYear; year >= 2020; year--)
            {
                LstYearBarChart.Add(year);
            }
            SelectedYearBarChart = currentYear;
        }
        public void LoadYearColumnChart()
        {
            LstYearColumnChart = new List<int>();
            int currentYear = DateTime.Now.Year;
            for (int year = currentYear; year >= 2020; year--)
            {
                LstYearColumnChart.Add(year);
            }
            SelectedYearColumnChart = currentYear;
        }
        public void LoadMonthColumnChart()
        {
            LstMonthColumnChart = new List<int>();
            for (int month = 1; month <= 12; month++)
            {
                LstMonthColumnChart.Add(month);
            }
            SelectedMonthColumnChart = DateTime.Now.Month;
        }
        public void LoadMonthPieChart()
        {
            LstMonthPieChart = new List<int>();
            for (int month = 1; month <= 12; month++)
            {
                LstMonthPieChart.Add(month);
            }
            SelectedMonthPieChart = DateTime.Now.Month;
        }
        public void LoadYearPieChart()
        {
            LstYearPieChart = new List<int>();
            int currentYear = DateTime.Now.Year;
            for (int year = currentYear; year >= 2020; year--)
            {
                LstYearPieChart.Add(year);
            }
            SelectedYearPieChart = currentYear;
        }
        public void LoadDeviceChartLine()
        {
            LstDeviceChartLine = new ObservableCollection<DeviceVM>(_services.GetDevicesList());
            SelectedDeviceChartLine = LstDeviceChartLine.FirstOrDefault();
        }
        #endregion
        #region [ Methods - Language ]
        public void UpdateTexts()
        {
            ChartBarText = _languageService.GetString("Compare energy consumption levels");
            ChartColumnText = _languageService.GetString("Monthly energy consumption");
            ChartPieText = _languageService.GetString("Energy consumption rate in 30 days");
            ChartLineText = _languageService.GetString("Energy consumption in 30 days");
            MonthText = _languageService.GetString("Month");
            YearText = _languageService.GetString("Year");
            DeviceNameText = _languageService.GetString("Name device");
        }

        #endregion
        #region [ Methods - Update Timer ]
        private void UpdateChartsOnTimerTick(object sender, EventArgs e)
        {
            Console.WriteLine("Timer ticked. Updating charts...");

            // 1. Cập nhật Chart Bar (Nếu có SelectedYearBarChart)
            if (SelectedYearBarChart != 0)
            {
                InitializeChartBarAsyncWrapper(SelectedYearBarChart);
            }

            // 2. Cập nhật Chart Column (Nếu có SelectedMonthColumnChart và SelectedYearColumnChart)
            if (SelectedMonthColumnChart != 0 && SelectedYearColumnChart != 0)
            {
                InitializeChartColumnAsyncWrapper(SelectedMonthColumnChart, SelectedYearColumnChart);
            }

            // 3. Cập nhật Chart Pie (Nếu có SelectedMonthPieChart và SelectedYearPieChart)
            if (SelectedMonthPieChart != 0 && SelectedYearPieChart != 0)
            {
                InitializeChartPieAsyncWrapper(SelectedMonthPieChart, SelectedYearPieChart);
            }

            // 4. Cập nhật Chart Line (Nếu có SelectedDeviceChartLine)
            if (SelectedDeviceChartLine != null)
            {
                InitializeChartLineAsyncWrapper(SelectedDeviceChartLine.devid);
            }

        }
        #endregion

        #region [ Language Texts ]
        [ObservableProperty] private string chartBarText;
        [ObservableProperty] private string chartColumnText;
        [ObservableProperty] private string chartPieText;
        [ObservableProperty] private string chartLineText;
        [ObservableProperty] private string monthText;
        [ObservableProperty] private string yearText;
        [ObservableProperty] private string deviceNameText;
        #endregion
    }
}
