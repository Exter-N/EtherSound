using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class ResourceConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string strParameter = parameter as string;

            object key = values[0];
            FrameworkElement element = (FrameworkElement)values[1];

            switch (strParameter)
            {
                case "N":
                    return element.TryFindResource(key);
                case "I":
                    return element.TryFindResource(key) ?? key;
                default:
                    return element.FindResource(key);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
