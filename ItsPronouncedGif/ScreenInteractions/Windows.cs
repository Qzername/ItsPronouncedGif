using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using AM = Avalonia.Media;

namespace ItsPronouncedGif.ScreenInteractions
{
    class Windows : SystemScreenHandler
    {
        //Cursor handling
        //https://stackoverflow.com/questions/6750056/how-to-capture-the-screen-and-mouse-pointer-using-windows-apis
        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINTAPI
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        const Int32 CURSOR_SHOWING = 0x00000001;

        //https://stackoverflow.com/questions/1483928/how-to-read-the-color-of-a-screen-pixel
        //https://stackoverflow.com/questions/10185120/c-sharp-capture-screen-to-8-bit-256-color-bitmap
        public override AM.Color[,] CaptureScreen(int x, int y, int width, int height)
        {
            Rectangle bounds = new Rectangle(x,y,width,height);

            using (Bitmap Temp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(Temp))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

                    if(screenSettings.ShowCursor)
                    {
                        CURSORINFO pci;
                        pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

                        Debug.WriteLine("uuw");

                        if (GetCursorInfo(out pci))
                        {
                            Debug.WriteLine("uww");
                            if (pci.flags == CURSOR_SHOWING)
                            {
                                Debug.WriteLine("uwu");
                                DrawIcon(g.GetHdc(), pci.ptScreenPos.x - x, pci.ptScreenPos.y - y, pci.hCursor);
                                g.ReleaseHdc();
                            }
                        }
                    }
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

        public override ScreenResolution GetScreenResolution()
        {
            return new ScreenResolution()
            {
                Width = GetSystemMetrics(0),
                Height = GetSystemMetrics(1)
            };
        }
    }
}
