using System;
using System.Globalization;
using System.Windows.Data;

namespace Electric_Meter.Converters
{
    public class BoolToPlayPauseTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isConnected = value is bool b && b;
            return isConnected ? "⏸" : "▶";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
