using EtherSound.Properties;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EtherSound.View.Converters
{
    public class VolumeIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(targetType, System.Convert.ToBoolean(values[0]), System.Convert.ToDouble(values[1]));
        }

        public static T Convert<T>(bool muted, double masterVolume)
        {
            return (T)Convert(typeof(T), muted, masterVolume);
        }

        public static object Convert(Type targetType, bool muted, double masterVolume)
        {
            if (typeof(Icon) == targetType)
            {
                switch (Convert(muted, masterVolume))
                {
                    case Case.Muted:
                    case Case.Level0:
                        return Resources.TrayMute;
                    case Case.Level1:
                        return Resources.TrayLevel1;
                    case Case.Level2:
                        return Resources.TrayLevel2;
                    case Case.Level3:
                        return Resources.TrayLevel3;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (typeof(ImageSource) == targetType)
            {
                ImageSourceConverter converter = new ImageSourceConverter();

                return converter.ConvertFromInvariantString("pack://application:,,,/View/" + Convert<string>(muted, masterVolume));
            }
            else if (typeof(string) == targetType)
            {
                switch (Convert(muted, masterVolume))
                {
                    case Case.Muted:
                    case Case.Level0:
                        return "Resources/mute24.png";
                    case Case.Level1:
                        return "Resources/low24.png";
                    case Case.Level2:
                        return "Resources/med24.png";
                    case Case.Level3:
                        return "Resources/hi24.png";
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        static Case Convert(bool muted, double masterVolume)
        {
            if (muted)
            {
                return Case.Muted;
            }

            if (masterVolume <= 0.0)
            {
                return Case.Level0;
            }
            else if (masterVolume < 0.325)
            {
                return Case.Level1;
            }
            else if (masterVolume < 0.655)
            {
                return Case.Level2;
            }
            else
            {
                return Case.Level3;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private enum Case
        {
            Muted,
            Level0,
            Level1,
            Level2,
            Level3,
        }
    }
}
