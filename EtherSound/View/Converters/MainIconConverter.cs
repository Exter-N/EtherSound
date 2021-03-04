using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Size = System.Drawing.Size;

namespace EtherSound.View.Converters
{
    public class MainIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isDark = System.Convert.ToBoolean(values[0]);
            FrameworkElement element = (FrameworkElement)values[1];

            return WpfToGdipHelper.Render((Geometry)element.FindResource("MainIcon"), isDark ? Brushes.White : Brushes.Black, new Size(24, 24), new Size(96, 96));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
