using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using AM = Avalonia.Media;

namespace ItsPronouncedGif.ScreenInteractions
{
    class Windows : ISystemScreenHandler
    {
        //https://stackoverflow.com/questions/1483928/how-to-read-the-color-of-a-screen-pixel
        //https://stackoverflow.com/questions/10185120/c-sharp-capture-screen-to-8-bit-256-color-bitmap
        public AM.Color[,] CaptureScreen(int x, int y, int width, int height)
        {
            Rectangle bounds = new Rectangle(x,y,width,height);

            using (Bitmap Temp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(Temp))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                var bmp = Temp.Clone(new Rectangle(0, 0, bounds.Width, bounds.Height), PixelFormat.Format8bppIndexed);

                AM.Color[,] final = new AM.Color[width, height];

                for (int ty = 0; ty < height; ty++)
                    for (int tx = 0; tx < width; tx++)
                    {
                        var SDcolor = bmp.GetPixel(tx, ty);
                        final[tx, ty] = AM.Color.FromRgb(SDcolor.R, SDcolor.G, SDcolor.B);
                    }

                return final;
            }
        }

        //https://stackoverflow.com/questions/43739898/c-sharp-screen-size-without-references-in-interactive
        [DllImport("User32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetSystemMetrics(int nIndex);

        public ScreenResolution GetScreenResolution()
        {
            return new ScreenResolution()
            {
                Width = GetSystemMetrics(0),
                Height = GetSystemMetrics(1)
            };
        }
    }
}
