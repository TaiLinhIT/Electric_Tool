using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using Electric_Meter.Core; // Thêm namespace chứa RelayCommand

namespace Electric_Meter.Models
{
    public class LeftClickCommandConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is ICommand command && values[1] != null)
            {
                // Lấy thông tin đối tượng từ `values[1]`
                return new RelayCommand(obj => command.Execute(values[1]), null);
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
