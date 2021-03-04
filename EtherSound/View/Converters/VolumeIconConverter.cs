using EtherSound.View.Effects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Size = System.Drawing.Size;
using Icon = System.Drawing.Icon;
using Brushes = System.Windows.Media.Brushes;

namespace EtherSound.View.Converters
{
    public class VolumeIconConverter : IMultiValueConverter
    {
        static Dictionary<string, Icon> icons;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(targetType, System.Convert.ToBoolean(values[0]), System.Convert.ToDouble(values[1]), values[2] as FrameworkElement);
        }

        public static T Convert<T>(bool muted, double masterVolume, FrameworkElement element = null)
        {
            return (T)Convert(typeof(T), muted, masterVolume, element);
        }

        public static object Convert(Type targetType, bool muted, double masterVolume, FrameworkElement element = null)
        {
            if (typeof(Icon) == targetType)
            {
                InitializeIcons();

                return icons[(CompositionHelper.SystemUsesLightTheme ? "Light." : "Dark.") + GetResourceKey(Convert(muted, masterVolume))];
            }
            else if (typeof(Geometry) == targetType)
            {
                string key = GetResourceKey(Convert(muted, masterVolume));
                
                return element.TryFindResource(key);
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

        static string GetResourceKey(Case @case)
        {
            switch (@case)
            {
                case Case.Muted:
                case Case.Level0:
                    return "VolumeNone";
                case Case.Level1:
                    return "VolumeLow";
                case Case.Level2:
                    return "VolumeMedium";
                case Case.Level3:
                    return "VolumeHigh";
                default:
                    throw new NotImplementedException();
            }
        }

        static void InitializeIcons()
        {
            if (null != icons)
            {
                return;
            }

            ResourceDictionary resources = new ResourceDictionary
            {
                Source = Helpers.MakePackUri("View/Resources.xaml")
            };

            icons = new Dictionary<string, Icon>();
            foreach (Case @case in Enum.GetValues(typeof(Case)))
            {
                string key = GetResourceKey(@case);
                if (!icons.ContainsKey("Dark." + key))
                {
                    icons.Add("Dark." + key, WpfToGdipHelper.RenderToIcon(
                        (Geometry)resources[key], Brushes.White, new Size(24, 24), new Size(48, 48)));
                }
                if (!icons.ContainsKey("Light." + key))
                {
                    icons.Add("Light." + key, WpfToGdipHelper.RenderToIcon(
                        (Geometry)resources[key], Brushes.Black, new Size(24, 24), new Size(48, 48)));
                }
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
