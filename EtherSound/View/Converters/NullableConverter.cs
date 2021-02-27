using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class NullableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToString(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (string.IsNullOrWhiteSpace((string)value))
                {
                    return Activator.CreateInstance(targetType);
                }
                else
                {
                    return Activator.CreateInstance(targetType, System.Convert.ChangeType(value, targetType.GetGenericArguments()[0]));
                }
            }
            else
            {
                return System.Convert.ChangeType(value, targetType);
            }
        }
    }
}
