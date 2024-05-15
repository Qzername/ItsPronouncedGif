namespace ItsPronouncedGif.ViewModels;

public class MainViewModel : ViewModelBase
{
    public void CompileGIF()
    {
        GifHandler gif = new GifHandler();

        gif.AddPicture();
        gif.Compile("");
    }
}
