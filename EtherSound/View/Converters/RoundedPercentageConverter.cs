using System;
using System.Globalization;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class RoundedPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(System.Convert.ToDouble(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public static string Convert(double value)
        {
            return Math.Round(value * 100.0).ToString();
        }
    }
}
