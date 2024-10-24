using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using AM = Avalonia.Media;

namespace ItsPronouncedGif.ScreenInteractions.Windows
{
    class WindowsBitmap : SystemScreenHandler
    {
        //Cursor handling
        //https://stackoverflow.com/questions/6750056/how-to-capture-the-screen-and-mouse-pointer-using-windows-apis
        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public nint hCursor;
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
        static extern bool DrawIcon(nint hDC, int X, int Y, nint hIcon);

        const int CURSOR_SHOWING = 0x00000001;

        //https://stackoverflow.com/questions/1483928/how-to-read-the-color-of-a-screen-pixel
        //https://stackoverflow.com/questions/10185120/c-sharp-capture-screen-to-8-bit-256-color-bitmap
        public override AM.Color[,] CaptureScreen(int x, int y, int width, int height)
        {
            Rectangle bounds = new Rectangle(x, y, width, height);

            using (Bitmap Temp = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(Temp))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

                    if (screenSettings.ShowCursor)
                    {
                        CURSORINFO pci;
                        pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

                        if (GetCursorInfo(out pci))
                        {
                            if (pci.flags == CURSOR_SHOWING)
                            {
                                DrawIcon(g.GetHdc(), pci.ptScreenPos.x - x, pci.ptScreenPos.y - y, pci.hCursor);
                                g.ReleaseHdc();
                            }
                        }
                    }
                }

                AM.Color[,] final = new AM.Color[width, height];

                BitmapData btmDt = Temp.LockBits(
                    new Rectangle(0, 0, Temp.Width, Temp.Height),
                    ImageLockMode.ReadOnly,
                    Temp.PixelFormat
                );

                nint pointer = btmDt.Scan0;
                int size = Math.Abs(btmDt.Stride) * Temp.Height;
                byte[] pixels = new byte[size];
                Marshal.Copy(pointer, pixels, 0, size);

                for (int w = 0; w < width; w++)
                    for (int h = 0; h < height; h++)
                    {
                        int index = (h * width + w) * 4;
                        final[w, h] = AM.Color.FromRgb(pixels[index + 2],
                                                       pixels[index + 1],
                                                       pixels[index]);
                    }

                Marshal.Copy(pixels, 0, pointer, size);
                Temp.UnlockBits(btmDt);

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
