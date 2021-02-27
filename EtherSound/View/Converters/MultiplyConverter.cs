using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class MultiplyConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value, CultureInfo.InvariantCulture) *
                   ((null != parameter) ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 1.0);
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double result = 1.0;
            foreach (object value in values)
            {
                result *= System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            if (null != parameter)
            {
                result *= System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value, CultureInfo.InvariantCulture) /
                   ((null != parameter) ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 1.0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
