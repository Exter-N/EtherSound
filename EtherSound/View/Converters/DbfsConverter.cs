using System;
using System.Globalization;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class DbfsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double dBFS = Math.Log10(System.Convert.ToDouble(value, CultureInfo.InvariantCulture)) * 20.0;

            if (parameter != null)
            {
                return string.Format("{0:N0} {1}", dBFS, parameter).Trim();
            }

            return dBFS;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Pow(10.0, System.Convert.ToDouble(value, CultureInfo.InvariantCulture) / 20.0);
        }
    }
}
