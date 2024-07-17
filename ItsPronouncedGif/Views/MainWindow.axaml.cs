using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;

namespace ItsPronouncedGif.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance;

    public MainWindow()
    {
        Instance = this;

        InitializeComponent();
    }

    public void ChangeSizeOfWindow(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void ChangeRecordingSettings(bool isRecording) 
    { 
        CanResize = !isRecording;
        Topmost = isRecording;
    }
}
