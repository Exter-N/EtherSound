using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EtherSound.View.Converters
{
    public class ColorAddendConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!CompositionHelper.SystemUsesLightTheme)
            {
                return Color.FromArgb(0, 0, 0, 0);
            }

            if (value is string strValue)
            {
                value = ColorConverter.ConvertFromString(strValue);
            }

            Color color = (Color)value;

            return Color.FromArgb(0, color.R, color.G, color.B);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
