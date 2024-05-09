using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ItsPronouncedGif
{
    public class GifHandler
    {
        Stream stream;

        public GifHandler()
        {
            stream = new FileStream("./test.gif",FileMode.Create);
        }

        public void AddPicture()
        {
        }

        public void Compile(string path)
        {

        }
    }
}
