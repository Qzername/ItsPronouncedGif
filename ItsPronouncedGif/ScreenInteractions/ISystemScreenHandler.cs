using Avalonia.Media;

namespace ItsPronouncedGif.ScreenInteractions
{
    interface ISystemScreenHandler
    {
        public Color[,] CaptureScreen(int x, int y, int width, int height);
        public ScreenResolution GetScreenResolution();
    }
}
