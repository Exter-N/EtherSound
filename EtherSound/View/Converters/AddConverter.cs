using System;
using System.Globalization;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class AddConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value) +
                   ((null != parameter) ? System.Convert.ToDouble(parameter) : 0.0);
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double result = 0.0;
            foreach (object value in values)
            {
                result += System.Convert.ToDouble(value);
            }
            if (null != parameter)
            {
                result += System.Convert.ToDouble(parameter);
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value) -
                   ((null != parameter) ? System.Convert.ToDouble(parameter) : 0.0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
