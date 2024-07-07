namespace ItsPronouncedGif.ViewModels;

public class MainViewModel : ViewModelBase
{
    public void CompileGIF()
    {
        GifHandler gif = new GifHandler(10,10);

        gif.AddPicture([
            1,1,1,1,1,2,2,2,2,2,
            1,1,1,1,1,2,2,2,2,2,
            1,1,1,1,1,2,2,2,2,2,
            1,1,1,0,0,0,0,2,2,2,
            1,1,1,0,0,0,0,2,2,2,
            2,2,2,0,0,0,0,1,1,1,
            2,2,2,0,0,0,0,1,1,1,
            2,2,2,2,2,1,1,1,1,1,
            2,2,2,2,2,1,1,1,1,1,
            2,2,2,2,2,1,1,1,1,1,
        ]);

        gif.Compile("./example.gif");
    }
}
