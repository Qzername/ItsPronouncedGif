using Avalonia.Media;
using ItsPronouncedGif.ScreenInteractions;
using ReactiveUI.Fody.Helpers;
using System.Diagnostics;

namespace ItsPronouncedGif.ViewModels;

public class MainViewModel : ViewModelBase
{
    [Reactive] int width { get; set; } = 5;
    [Reactive] int height { get; set; } = 5;
    [Reactive] int x { get; set; } = 100;
    [Reactive] int y { get; set; } = 100;

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
        gif.Compile("./result.gif");
        isTextBoxesEnabled = true;
    }
}
