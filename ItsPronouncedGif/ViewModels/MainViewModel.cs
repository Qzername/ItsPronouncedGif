using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
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

    [Reactive] bool prerollActive { get; set; } = true;
    [Reactive] int prerollTime { get; set; } = 5;

    [Reactive] bool showPrerollCounter { get; set; } = false;
    [Reactive] int prerollTimerCurrent { get; set; } = 0;

    [Reactive] bool showCursor { get; set; } = true;

    [Reactive] bool isTextBoxesEnabled { get; set; } = true;
    [Reactive] bool isRecording { get; set; } = false;

    GifCreator gif;
    Screen screen;

    Task recording;

    //this is stupid way to detect either when was width changed by resizing window
    //or by manually writting width and height into textboxes
    bool manualChange = false;

    string fileDir;

    public MainViewModel()
    {
        screen = new Screen();

        MainWindow.Instance.Resized += WindowResized;
    }

    public async void Record()
    {
        string fileDir = await FileSelection();

        if (fileDir == string.Empty)
            return;

        this.fileDir = fileDir;

        gif = new GifCreator(Width - 10, Height - 10);

        isRecording = true;
        MainWindow.Instance.ChangeRecordingSettings(isRecording);
        
        screen.SwitchShowCursor(showCursor);

        recording = new Task(async () =>
        {
            //preroll handling
            showPrerollCounter = true;

            prerollTimerCurrent = prerollActive ? prerollTime : 0;

            while(prerollTimerCurrent > 0)
            {
                prerollTimerCurrent--;
                await Task.Delay(1000);
            }

            showPrerollCounter = false;
            isTextBoxesEnabled = false;

            //main recording
            int fpsToFpsDuration = Convert.ToInt32(1000f / maxFPS);
            currentFPS = maxFPS;

            while (isRecording)
            {
                var frameStart = DateTime.Now;

                var pos = MainWindow.Instance.Position;

                gif.AddPicture(screen.CaptureScreen(pos.X + 5, pos.Y + 25, Width - 10, Height - 10), Convert.ToInt32(1000/currentFPS));

                var frameEnd = DateTime.Now;

                var diff = frameEnd - frameStart;

                //check the time to the next frame and delay task when needed
                int remaningTime = fpsToFpsDuration - diff.Milliseconds;

                if (remaningTime > 0) 
                    await Task.Delay(remaningTime);

                int ms = (DateTime.Now - frameStart).Milliseconds;

                if (ms == 0)
                    continue;

                currentFPS = 1000 / ms;
            }
        });
            
        recording.Start();
    }

    public void StopRecord()
    {
        isRecording = false;
        MainWindow.Instance.ChangeRecordingSettings(isRecording);

        if (showPrerollCounter)
        {
            showPrerollCounter = false;
            return;
        }

        gif.Compile(fileDir);

        isTextBoxesEnabled = true;
    }

    public void OpenCredits()
    {
        new Credits().ShowDialog(MainWindow.Instance);
    }

    void WindowResized(object? sender, WindowResizedEventArgs e)
    {
        //i had to do this because program has to detect
        //either the resize was caused by textbox or dragging edge of the window
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

    async Task<string> FileSelection()
    {
        string result = string.Empty;

        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var file = await desktop.MainWindow!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Save gif recording as...",
                DefaultExtension = "gif",
                ShowOverwritePrompt = true,
            });

            if (file is null)
                return string.Empty;

            return file.Path.AbsolutePath;
        }

        return string.Empty;
    }

    void ChangeWindowSize(int width, int height)
    {
        manualChange = true;
        MainWindow.Instance.ChangeSizeOfWindow(width + 200, height + 20);
    }
}
