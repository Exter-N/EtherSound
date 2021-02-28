using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace EtherSound
{
    // Copied from or heavily inspired by :
    // https://blogs.msdn.microsoft.com/adam_nathan/2006/05/04/aero-glass-inside-a-wpf-window/
    // https://github.com/bbougot/AcrylicWPF/blob/master/MainWindow.xaml.cs
    // And EarTrumpet, released under MIT License, (c) 2015 EarTrumpet's authors
    // https://github.com/File-New-Project/EarTrumpet/blob/master/EarTrumpet/Extensions/BlurWindowExtensions.cs
    // Files in https://github.com/File-New-Project/EarTrumpet/tree/master/EarTrumpet/Services
    static class CompositionHelper
    {
        private const byte TransparentBackgroundAlpha = 204;
        private const byte OpaqueAlpha = 255;

        [DllImport("dwmapi.dll", PreserveSig = false)]
        static extern void DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        static extern bool DwmIsCompositionEnabled();

        [DllImport("user32.dll", PreserveSig = false)]
        static extern void SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("uxtheme.dll", EntryPoint = "#98", CharSet = CharSet.Unicode)]
        static extern uint GetImmersiveUserColorSetPreference(bool bForceCheckRegistry, bool bSkipCheckOnFail);

        [DllImport("uxtheme.dll", EntryPoint = "#96", CharSet = CharSet.Unicode)]
        static extern uint GetImmersiveColorTypeFromName(string name);

        [DllImport("uxtheme.dll", EntryPoint = "#95", CharSet = CharSet.Unicode)]
        static extern uint GetImmersiveColorFromColorSetEx(uint dwImmersiveColorSet, uint dwImmersiveColorType, bool bIgnoreHighContrast, uint dwHighContrastCacheMode);

        public static bool IsTransparencyEnabled
        {
            get
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                {
                    return Convert.ToInt32(baseKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").GetValue("EnableTransparency", 0)) > 0;
                }
            }
        }

        public static bool UseAccentColor
        {
            get
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                {
                    return Convert.ToInt32(baseKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize").GetValue("ColorPrevalence", 0)) > 0;
                }
            }
        }

        public static Color Background
        {
            get
            {
                if (SystemParameters.HighContrast)
                {
                    return GetImmersiveColor("ImmersiveApplicationBackground", OpaqueAlpha);
                }
                else if (UseAccentColor)
                {
                    return IsTransparencyEnabled
                        ? GetImmersiveColor("ImmersiveSystemAccentDark2", TransparentBackgroundAlpha)
                        : GetImmersiveColor("ImmersiveSystemAccentDark1", OpaqueAlpha);
                }
                else
                {
                    return GetImmersiveColor("ImmersiveDarkChromeMedium", IsTransparencyEnabled ? TransparentBackgroundAlpha : OpaqueAlpha);
                }
            }
        }

        public static Color GetImmersiveColor(string name, byte? alpha = null)
        {
            uint colorSet = GetImmersiveUserColorSetPreference(false, false);
            uint colorType = GetImmersiveColorTypeFromName(name);

            uint color = GetImmersiveColorFromColorSetEx(colorSet, colorType, false, 0);
            return Color.FromArgb(alpha.GetValueOrDefault((byte)((color & 0xFF000000u) >> 24)), (byte)(color & 0xFFu), (byte)((color & 0xFF00u) >> 8), (byte)((color & 0xFF0000u) >> 16));
        }

        public static void UpdateResources(ResourceDictionary resources)
        {
            UpdateSolidColorBrushResource(resources, "WindowBackground", Background);
            UpdateSolidColorBrushResource(resources, "WindowForeground", GetImmersiveColor("ImmersiveApplicationTextDarkTheme"));
            UpdateSolidColorBrushResource(resources, "HeaderBackground", GetImmersiveColor("ImmersiveSystemAccent", 51));
            UpdateSolidColorBrushResource(resources, "CottonSwabSliderThumb", GetImmersiveColor("ImmersiveSystemAccent"));
            UpdateSolidColorBrushResource(resources, "CottonSwabSliderTrackFill", GetImmersiveColor("ImmersiveSystemAccentLight1"));
            UpdateSolidColorBrushResource(resources, "CottonSwabSliderThumbHover", GetImmersiveColor("ImmersiveControlDarkSliderThumbHover"));
            UpdateSolidColorBrushResource(resources, "CottonSwabSliderThumbPressed", GetImmersiveColor("ImmersiveControlDarkSliderThumbHover"));
        }

        static bool UpdateSolidColorBrushResource(ResourceDictionary resources, object key, Color color)
        {
            if (resources.Contains(key))
            {
                ((SolidColorBrush)resources[key]).Color = color;

                return true;
            }

            foreach (ResourceDictionary merged in resources.MergedDictionaries)
            {
                if (UpdateSolidColorBrushResource(merged, key, color))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ExtendGlassFrame(Window window, Thickness? margin)
        {
            if (!DwmIsCompositionEnabled())
            {
                return false;
            }
            
            IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();

            // Set the background to transparent from both the WPF and Win32 perspectives
            // window.Background = Brushes.Transparent;
            HwndSource.FromHwnd(hwnd).CompositionTarget.BackgroundColor = Colors.Transparent;

            Margins margins = new Margins(margin.GetValueOrDefault(Margins.NullThickness));
            DwmExtendFrameIntoClientArea(hwnd, ref margins);

            return true;
        }

        public static void EnableBlurBehind(Window window)
        {
            AccentPolicy accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND,
                AccentFlags = AccentFlags.DrawLeftBorder | AccentFlags.DrawTopBorder
            };

            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);

                WindowCompositionAttributeData data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };

                SetWindowCompositionAttribute(new WindowInteropHelper(window).EnsureHandle(), ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }

        private enum AccentState
        {
            ACCENT_DISABLED = 1,
            ACCENT_ENABLE_GRADIENT = 0,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        [Flags]
        private enum AccentFlags
        {
            // ...
            DrawLeftBorder = 0x20,
            DrawTopBorder = 0x40,
            DrawRightBorder = 0x80,
            DrawBottomBorder = 0x100,
            DrawAllBorders = (DrawLeftBorder | DrawTopBorder | DrawRightBorder | DrawBottomBorder)
            // ...
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public AccentFlags AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        private enum WindowCompositionAttribute
        {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Margins
        {
            public static readonly Thickness NullThickness = new Thickness(-1.0);

            public int Left;
            public int Right;
            public int Top;
            public int Bottom;

            public Margins(Thickness t)
            {
                Left = (int)t.Left;
                Right = (int)t.Right;
                Top = (int)t.Top;
                Bottom = (int)t.Bottom;
            }
        }
    }
}
