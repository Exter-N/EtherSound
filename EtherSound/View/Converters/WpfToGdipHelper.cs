using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Gdip = System.Drawing;
using Imaging = System.Drawing.Imaging;
using Brush = System.Windows.Media.Brush;

namespace EtherSound.View.Converters
{
    static class WpfToGdipHelper
    {
        public static BitmapSource Render(Geometry geometry, Brush brush, Gdip.Size originalSize, Gdip.Size size)
        {
            Path path = new Path
            {
                Data = geometry,
                Fill = brush,
                Width = originalSize.Width,
                Height = originalSize.Height,
            };
            Viewbox vbox = new Viewbox
            {
                Width = size.Width,
                Height = size.Height,
                Child = path,
            };

            vbox.Measure(new Size(size.Width, size.Height));
            vbox.Arrange(new Rect(0.0, 0.0, size.Width, size.Height));

            RenderTargetBitmap bitmap = new RenderTargetBitmap(size.Width, size.Height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(vbox);

            return bitmap;
        }

        static Gdip.Bitmap ConvertBitmap(BitmapSource source)
        {
            Gdip.Bitmap gdipBitmap = new Gdip.Bitmap(source.PixelWidth, source.PixelHeight, Imaging.PixelFormat.Format32bppPArgb);
            try
            {
                Imaging.BitmapData data = gdipBitmap.LockBits(new Gdip.Rectangle(0, 0, gdipBitmap.Width, gdipBitmap.Height), Imaging.ImageLockMode.WriteOnly, Imaging.PixelFormat.Format32bppPArgb);
                try
                {
                    source.CopyPixels(new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight), data.Scan0, data.Stride * data.Height, data.Stride);
                }
                finally
                {
                    gdipBitmap.UnlockBits(data);
                }

                return gdipBitmap;
            }
            catch
            {
                gdipBitmap.Dispose();
                throw;
            }
        }

        public static Gdip.Icon RenderToIcon(Geometry geometry, Brush brush, Gdip.Size originalSize, Gdip.Size size)
        {
            BitmapSource source = Render(geometry, brush, originalSize, size);
            using (Gdip.Bitmap bitmap = ConvertBitmap(source))
            {
                return Gdip.Icon.FromHandle(bitmap.GetHicon());
            }
        }
    }
}
