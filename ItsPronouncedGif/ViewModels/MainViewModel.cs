using Avalonia.Media;
using Avalonia.Media.Imaging;
using ItsPronouncedGif.ScreenInteractions;
using ReactiveUI.Fody.Helpers;
using System.Diagnostics;
using System.Threading.Tasks;

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
    [Reactive] bool isRecording { get; set; } = false;

    volatile bool vIsRecording;

    volatile GifCreator gif;
    volatile Screen screen;

    Task recording;

    public MainViewModel()
    {
        screen = new Screen();
    }

    public void AddFrame()
    {
        CreateGifCreator();
        gif.AddPicture(screen.CaptureScreen(x, y, width, height));
    }

    public void Record()
    {
        CreateGifCreator();

        isRecording = true;
        vIsRecording = true;

        recording = new Task(() =>
        {
            while(vIsRecording)
            {
                gif.AddPicture(screen.CaptureScreen(x, y, width, height));
                await Task.Delay(1000);
            }
        });

        recording.Start();
    }

    public void StopRecord()
    {
        isRecording = false;
        vIsRecording = false;
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

    void CreateGifCreator()
    {
        if (isTextBoxesEnabled)
        {
            gif = new GifCreator(width, height);
            isTextBoxesEnabled = false;
        }
    }
}
