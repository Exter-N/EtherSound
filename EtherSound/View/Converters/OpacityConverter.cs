using System;
using System.Globalization;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class OpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (System.Convert.ToBoolean(value) ? 1.0 : 0.5) * ((null == parameter) ? 1.0 : System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
