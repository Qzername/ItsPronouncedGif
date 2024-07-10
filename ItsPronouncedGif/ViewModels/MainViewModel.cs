using Avalonia.Media;
using Avalonia.Media.Imaging;
using ItsPronouncedGif.ScreenInteractions;
using ReactiveUI.Fody.Helpers;

namespace ItsPronouncedGif.ViewModels;

public class MainViewModel : ViewModelBase
{
    [Reactive] int width { get; set; } = 493;
    [Reactive] int height { get; set; } = 493;
    [Reactive] int x { get; set; } = 100;
    [Reactive] int y { get; set; } = 100;

    [Reactive] Bitmap? result { get; set; }
    [Reactive] bool showResultError { get; set; } = false;

    [Reactive] bool isTextBoxesEnabled { get; set; } = true;

    GifCreator gif;
    Screen screen;

    public MainViewModel()
    {
        screen = new Screen();
    }

    public void AddFrame()
    {
        if (isTextBoxesEnabled)
        {
            gif = new GifCreator(width, height);
            isTextBoxesEnabled = false;
        }

        gif.AddPicture(screen.CaptureScreen(x, y, width, height));
    }

    public void Compile()
    {
        showResultError = false;

        gif.Compile("./result.gif");
        isTextBoxesEnabled = true;

        try
        {
            result = new Bitmap("./result.gif");
        }
        catch(System.Exception)
        {
            showResultError= true;
        }
    }
}
