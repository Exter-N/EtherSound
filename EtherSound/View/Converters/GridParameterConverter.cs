using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    class GridParameterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strParameter = (parameter as string) ?? string.Empty;

            bool isTruthy = IsTruthy(value, strParameter);

            int baseValue = 0;

            if (strParameter.IndexOf('I') >= 0)
            {
                isTruthy = !isTruthy;
            }

            if (strParameter.IndexOf('1') >= 0)
            {
                baseValue = 1;
            }

            return baseValue + (isTruthy ? 1 : 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        static bool IsTruthy(object value, string parameter)
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
            else if (value is bool boolValue)
            {
                return boolValue;
            }

            return value != null;
        }
    }
}
