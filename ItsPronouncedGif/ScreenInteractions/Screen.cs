using Avalonia.Media;
using System;

namespace ItsPronouncedGif.ScreenInteractions
{
    /// <summary>
    /// Class for getting information from screen from current operating system
    /// </summary>
    public class Screen
    {
        SystemScreenHandler currentHandler;

        public Screen()
        {
            if (OperatingSystem.IsWindows())
                currentHandler = new Windows();
            else
                throw new Exception("Current operating system is not supported (yet)");
        }

        public Color[,] CaptureScreen(int x, int y, int width, int height) => currentHandler.CaptureScreen(x, y, width, height);
        public ScreenResolution GetResolution() => currentHandler.GetScreenResolution();
        public void SwitchShowCursor(bool showCursor)
        {
            currentHandler.SetSettings(new ScreenSettings()
            {
                ShowCursor = showCursor
            });
        }
    }
}