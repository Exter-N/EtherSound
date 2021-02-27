using System;
using System.Globalization;
using System.Windows.Data;

namespace EtherSound.View.Converters
{
    public class BinarySizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long size = System.Convert.ToInt64(value);
            if (size < (1 << 10))
            {
                return string.Format("{0} {1}", size, parameter);
            }
            else if (size < (1 << 20))
            {
                return string.Format("{0:G3} ki{1}", size / (double)(1 << 10), parameter);
            }
            else if (size < (1 << 30))
            {
                return string.Format("{0:G3} Mi{1}", size / (double)(1 << 20), parameter);
            }
            else
            {
                return string.Format("{0:G3} Gi{1}", size / (double)(1 << 30), parameter);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
