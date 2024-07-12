using Avalonia.Controls;

namespace ItsPronouncedGif.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance;

    public MainWindow()
    {
        Instance = this;

        InitializeComponent();
    }
}
