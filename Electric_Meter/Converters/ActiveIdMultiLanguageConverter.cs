using System.Globalization;
using System.Windows.Data;

namespace Electric_Meter.Converters
{
    public class ActiveIdMultiLanguageConverter : IMultiValueConverter
    {
        // values[0] = activeid, values[1] = ActiveCommandText, values[2] = InActiveCommandText
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3) return "";
            if (values[0] == null) return "";

            int activeId = 0;
            if (!int.TryParse(values[0].ToString(), out activeId)) return "";

            string activeText = values[1]?.ToString() ?? "Hoạt động";
            string inActiveText = values[2]?.ToString() ?? "Không hoạt động";

            return activeId == 1 ? activeText : inActiveText;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string str = value?.ToString();
            if (str == null) return new object[] { 0, null, null };

            // Không hỗ trợ convert back
            return new object[] { 0, null, null };
        }
    }
}
