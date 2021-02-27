using System;
using System.Windows;
using System.Windows.Forms;
using FColorDialog = System.Windows.Forms.ColorDialog;
using DColor = System.Drawing.Color;
using MColor = System.Windows.Media.Color;

namespace EtherSound.View
{
    static class ColorDialog
    {
        public static MColor? Show(Window owner, MColor defaultColor)
        {
            using (FColorDialog colorDialog = new FColorDialog())
            {
                colorDialog.AnyColor = true;
                colorDialog.FullOpen = true;
                colorDialog.Color = DColor.FromArgb(defaultColor.A, defaultColor.R, defaultColor.G, defaultColor.B);
                if (colorDialog.ShowDialog((null != owner) ? new Win32Window(owner) : null) != DialogResult.OK)
                {
                    return null;
                }

                DColor color = colorDialog.Color;

                return MColor.FromArgb(color.A, color.R, color.G, color.B);
            }
        }
    }
}
