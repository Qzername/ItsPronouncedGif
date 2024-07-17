using Avalonia.Media;

namespace ItsPronouncedGif.ScreenInteractions
{
    public abstract class SystemScreenHandler
    {
        protected ScreenSettings screenSettings;

        public abstract Color[,] CaptureScreen(int x, int y, int width, int height);
        public abstract ScreenResolution GetScreenResolution();
        public void SetSettings(ScreenSettings screenSettings)
        {
            this.screenSettings = screenSettings;
        }
    }
}
