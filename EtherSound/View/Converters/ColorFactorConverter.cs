using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EtherSound.View.Converters
{
    public class ColorFactorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                value = ColorConverter.ConvertFromString(strValue);
            }

            Color color = (Color)value;

            return CompositionHelper.SystemUsesLightTheme
                ? Color.FromArgb(0xFF, (byte)(0xFF ^ color.R), (byte)(0xFF ^ color.G), (byte)(0xFF ^ color.B))
                : Color.FromArgb(0xFF, color.R, color.G, color.B);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
