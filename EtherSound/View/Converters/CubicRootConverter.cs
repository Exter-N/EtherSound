using System;
using System.Globalization;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class CubicRootConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Pow(System.Convert.ToDouble(value, CultureInfo.InvariantCulture), 1.0 / 3.0) *
                ((null != parameter) ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 1.0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Pow(System.Convert.ToDouble(value, CultureInfo.InvariantCulture) /
                ((null != parameter) ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 1.0), 3.0);
        }
    }
}
