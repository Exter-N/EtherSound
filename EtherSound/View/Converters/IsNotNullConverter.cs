using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class IsNotNullConverter : IValueConverter
    {
        VisibilityConverter visibilityConverter;

        VisibilityConverter VisibilityConverter => visibilityConverter ?? (visibilityConverter = new VisibilityConverter());

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strParameter = (parameter as string) ?? string.Empty;

            bool isNotNull = IsNotNull(value, strParameter);

            if (targetType == typeof(Visibility))
            {
                return VisibilityConverter.Convert(isNotNull, targetType, parameter, culture);
            }

            if (strParameter.IndexOf('I') >= 0)
            {
                isNotNull = !isNotNull;
            }

            return isNotNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        static bool IsNotNull(object value, string parameter)
        {
            if (value is string strValue)
            {
                if (parameter.IndexOf('W') >= 0)
                {
                    return !string.IsNullOrWhiteSpace(strValue);
                }
                else if (parameter.IndexOf('E') >= 0)
                {
                    return !string.IsNullOrEmpty(strValue);
                }
            }
            
            return value != null;
        }
    }
}
