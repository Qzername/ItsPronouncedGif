using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;

namespace ItsPronouncedGif.Views
{
    public partial class Credits : Window
    {
        public Credits()
        {
            InitializeComponent();
        }

        public void ClickHandler(object sender, RoutedEventArgs args)
        {
            Process.Start(new ProcessStartInfo { FileName = "https://github.com/Qzername/ItsPronouncedGif", UseShellExecute = true });
        }
    }
}
