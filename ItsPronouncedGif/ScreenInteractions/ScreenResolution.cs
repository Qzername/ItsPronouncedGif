using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItsPronouncedGif.ScreenInteractions
{
    public struct ScreenResolution(int Width, int Height)
    {
        public int Width = Width;
        public int Height = Height;
    }
}
