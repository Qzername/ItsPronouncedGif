using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ItsPronouncedGif.ScreenInteractions;
using ItsPronouncedGif.Views;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ItsPronouncedGif.ViewModels;

public class MainViewModel : ViewModelBase
{
    int _width;
    public int Width
    {
        get => _width;
        set
        {
            this.RaiseAndSetIfChanged(ref _width, value);
            ChangeWindowSize(Width, Height);
        }
    }

    int _height;
    public int Height
    {
        get => _height;
        set
        {
            this.RaiseAndSetIfChanged(ref _height, value);
            ChangeWindowSize(Width, Height);
        }
    }

    [Reactive] int maxFPS { get; set; } = 10;
    [Reactive] float currentFPS { get; set; } = 0;

    [Reactive] bool isTextBoxesEnabled { get; set; } = true;
    [Reactive] bool isRecording { get; set; } = false;

    GifCreator gif;
    Screen screen;

    Task recording;

    //this is stupid way to detect either when was width changed by resizing window
    //or by manually writting width and height into textboxes
    bool manualChange = false;

    public MainViewModel()
    {
        screen = new Screen();

        MainWindow.Instance.Resized += WindowResized;
    }

    public void Record()
    {
        if (isTextBoxesEnabled)
        {
            gif = new GifCreator(Width-10, Height-10);
            isTextBoxesEnabled = false;
        }

        isRecording = true;

        recording = new Task(async () =>
        {
            int fpsToFpsDuration = Convert.ToInt32(1000f / maxFPS);

            while(isRecording)
            {
                var frameStart = DateTime.Now;

                var pos = MainWindow.Instance.Position;
                gif.AddPicture(screen.CaptureScreen(pos.X + 5, pos.Y + 25, Width - 10, Height - 10));

                var frameEnd = DateTime.Now;

                var diff = frameEnd - frameStart;
                int remaningTime = fpsToFpsDuration - diff.Milliseconds;

                if (remaningTime > 0)
                    await Task.Delay(remaningTime);

                currentFPS = 1000 / (DateTime.Now - frameStart).Milliseconds;
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

    private void WindowResized(object? sender, WindowResizedEventArgs e)
    {
        if(manualChange)
        {
            manualChange = false;
            return;
        }

        var b = MainWindow.Instance.Bounds;

        _width = Convert.ToInt32(b.Width) - 200;
        _height = Convert.ToInt32(b.Height) - 20;

        this.RaisePropertyChanged(nameof(Width));
        this.RaisePropertyChanged(nameof(Height));
    }

    void ChangeWindowSize(int width, int height)
    {
        manualChange = true;
        MainWindow.Instance.ChangeSizeOfWindow(width + 200, height + 20);
    }
}
