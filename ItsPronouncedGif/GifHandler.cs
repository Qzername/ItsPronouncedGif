using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ItsPronouncedGif
{
    //See: https://www.oreilly.com/library/view/programming-web-graphics/1565924789/ch01s02.html
    public class GifHandler
    {
        Stream stream;

        List<int> picture;

        public GifHandler()
        {
            stream = new FileStream("./test.gif",FileMode.Create);
        }

        public void AddPicture()
        {
            picture = new List<int>();
        }

        public void Compile(string path)
        {
            BinaryWriter writer = new BinaryWriter(stream);

            //Header - version of gif
            writer.Write(new char[3] { 'G', 'I', 'F' });
            writer.Write(new char[3] { '8', '9', 'a' });

            // --- LSD ---

            //width and height
            writer.Write((short)5);
            writer.Write((short)3);

            //Color table information
            BitArray cti = new BitArray(new byte[1]);

            //Color table requirement
            cti[0] = true; 
            cti[1] = false;
            cti[2] = false;

            //Is sorted
            cti[3] = false; 

            //Bits per sample
            cti[4] = true;
            cti[5] = false;
            cti[6] = false;

            //Global Color Table
            cti[7] = true;

            byte[] b = new byte[1]; 
            cti.CopyTo(b,0);
            writer.Write(b);

            //Background color index
            writer.Write(Convert.ToByte(3));

            //Pixel aspect ratio
            writer.Write(Convert.ToByte(0));

            //  --- GCT --- 
            writer.Write((byte)255);//w
            writer.Write((byte)255);
            writer.Write((byte)255);
            writer.Write((byte)0);//b
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)255);//r
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);//g
            writer.Write((byte)255);
            writer.Write((byte)0);

            // --- Picture ---
            //Picture descriptor
            writer.Write(Convert.ToByte(0x2c)); //img separator character
            writer.Write(Convert.ToInt16(0)); //img left position
            writer.Write(Convert.ToInt16(0)); //img top positon                    
            writer.Write(Convert.ToInt16(5)); //img width                    
            writer.Write(Convert.ToInt16(3)); //img height
                                                 
            var desc = new BitArray(new byte[1]);
            desc[0] = false; // local color table present
            desc[1] = false; // img not interlaced
            desc[2] = false; // sort flag
            desc[5] = false; // size of local color table
            desc[6] = false;
            desc[7] = false;

            b = new byte[1];
            desc.CopyTo(b, 0);
            writer.Write(b);
            b = [0];

            //Picture data
            //DEBUG: EXAMPLE DATA
            int[] pixelData =
            [
               1,
               1,
               1,
               1,
               1,
               2,
               2,
               2,
               2,
               2,
               1,
               1,
               1,
               1,
               1,
            ];

            //getting lzw min code
            int max = pixelData.Max();
            int minCode = Convert.ToInt32(Math.Floor(Math.Log2(max)));

            if (minCode < 2)
                minCode = 2;

            var data = LZWCompression(pixelData, minCode);
            
            writer.Write(Convert.ToByte(minCode)); //lzw minimum code size
            writer.Write(Convert.ToByte(b.Length)); //number of bytes in sub-block
          
            for (int i = 0; i < b.Length; i++)
                writer.Write(b[i]);

            writer.Write(Convert.ToByte(0x3b)); //End of GIF

            writer.Close();
            stream.Close();
        }

        int[] LZWCompression(int[] data, int minCode)
        {
            List<string> codes = new List<string>();
            List<int> compressed = new List<int>();

            int colorsAmount = Convert.ToInt32(Math.Pow(2, minCode));

            int clearCode = colorsAmount;
            int EOIcode = colorsAmount + 1;

            //filling color codes wtih additional codes
            for (int i = 0; i < colorsAmount + 2; i++)
                codes.Add(i.ToString());
            
            //send clear code
            compressed.Add(clearCode);


            string buffer = data[0].ToString();

            for(int i = 1; i < data.Length; i++)
            {
                var k = data[i];
                string strK = $" {k}";

                if(codes.Contains(buffer + strK))
                    buffer += strK;
                else
                {
                    codes.Add(buffer + strK);
                    compressed.Add(codes.IndexOf(buffer));
                    buffer = k.ToString();
                }
            }

            compressed.Add(codes.IndexOf(buffer));
            compressed.Add(EOIcode);

            return compressed.ToArray();
        }
    }
}
