using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class IsNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strParameter = parameter as string;
            bool invert = strParameter == "I" || strParameter == "IC";

            if (targetType == typeof(Visibility))
            {
                bool collapse = strParameter == "C" || strParameter == "IC";

                return (value != null ^ invert) ? Visibility.Visible : (collapse ? Visibility.Collapsed : Visibility.Hidden);
            }

            return value != null ^ invert;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
