using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strParameter = parameter as string;
            bool invert = strParameter == "I" || strParameter == "IC";
            bool collapse = strParameter == "C" || strParameter == "IC";

            return ((bool)value ^ invert) ? Visibility.Visible : (collapse ? Visibility.Collapsed : Visibility.Hidden);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
