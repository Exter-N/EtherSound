using System;
using System.Globalization;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class StringFormatConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Format(System.Convert.ToString(parameter), value);
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Format(System.Convert.ToString(parameter), values);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
