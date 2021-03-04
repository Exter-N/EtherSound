using System;
using System.Globalization;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class RoundedPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(System.Convert.ToDouble(value), System.Convert.ToString(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public static string Convert(double value, string parameter)
        {
            return string.Format("{0} {1}", Math.Round(value * 100.0), parameter).Trim();
        }
    }
}
