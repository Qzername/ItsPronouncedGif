using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ItsPronouncedGif.ScreenInteractions;
using ItsPronouncedGif.Views;
using ReactiveUI.Fody.Helpers;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ItsPronouncedGif.ViewModels;

public class MainViewModel : ViewModelBase
{
    [Reactive] int width { get; set; } = 600;
    [Reactive] int height { get; set; } = 500;

    [Reactive] bool isTextBoxesEnabled { get; set; } = true;
    [Reactive] bool isRecording { get; set; } = false;

    GifCreator gif;
    Screen screen;

    Task recording;

    public MainViewModel()
    {
        screen = new Screen();
    }

    public void Record()
    {
        if (isTextBoxesEnabled)
        {
            gif = new GifCreator(width-10, height-10);
            isTextBoxesEnabled = false;
        }

        isRecording = true;

        recording = new Task(() =>
        {
            while(isRecording)
            {
                var pos = MainWindow.Instance.Position;

                gif.AddPicture(screen.CaptureScreen(pos.X + 5, pos.Y + 25, width - 10, height - 10));
            }
        });

        recording.Start();
    }

    public void StopRecord()
    {
        isRecording = false;

        gif.Compile("./result.gif");

        isTextBoxesEnabled = true;
    }
}
