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
            string strParameter = (parameter as string) ?? string.Empty;
            bool invert = strParameter.IndexOf('I') >= 0;
            bool collapse = strParameter.IndexOf('C') >= 0;

            return (System.Convert.ToBoolean(value) ^ invert) ? Visibility.Visible : (collapse ? Visibility.Collapsed : Visibility.Hidden);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
