using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ItsPronouncedGif
{
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

            //See: https://en.wikipedia.org/wiki/GIF
            //Also: https://www.oreilly.com/library/view/programming-web-graphics/1565924789/ch01s02.html

            //Header - version of gif
            writer.Write("GIF89a");

            // --- LSD ---
            
            //width and height
            writer.Write(3);
            writer.Write(5);

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
            writer.Write(Convert.ToByte(0));

            //Pixel aspect ratio
            writer.Write(Convert.ToByte(0)); 

            //  --- GCT --- 
            writer.Write(Convert.ToByte(255)); // white
            writer.Write(Convert.ToByte(255));
            writer.Write(Convert.ToByte(255));
            writer.Write(Convert.ToByte(255)); // red
            writer.Write(Convert.ToByte(0));
            writer.Write(Convert.ToByte(0));
            writer.Write(Convert.ToByte(0)); // blue
            writer.Write(Convert.ToByte(0));
            writer.Write(Convert.ToByte(255));
            writer.Write(Convert.ToByte(0)); // black
            writer.Write(Convert.ToByte(0));
            writer.Write(Convert.ToByte(0));

            // --- Picture ---
            //Picture descriptor
            writer.Write(Convert.ToByte(0x2c)); //img separator character
            writer.Write(Convert.ToInt16(0)); //img left position
            writer.Write(Convert.ToInt16(0)); //img top positon                    
            writer.Write(Convert.ToInt16(3)); //img width                    
            writer.Write(Convert.ToInt16(5)); //img height
                                                 
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

            //Picture data
            //DEBUG: EXAMPLE DATA
            List<int> pixelData =
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

            string result = LZW(pixelData);
            b = GetBytes(result);
            
            writer.Write(Convert.ToByte(2)); //lzw minimum code size
            writer.Write(Convert.ToByte(b.Length)); //number of bytes in sub-block
            
            for (int i = 0; i < b.Length; i++)
                writer.Write(b[i]);

            writer.Write(Convert.ToByte(0));
            writer.Write(Convert.ToByte(0x3b)); //End of GIF
        }

        // Code from below is taken from: https://gist.github.com/reZach/28247e4616c2596fc282b6167740e3f8

        // https://rosettacode.org/wiki/LZW_compression#C.23
        static string LZW(List<int> pixelImageData)
        {
            // build the dictionary
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            for (int i = 0; i < 4; i++)
                dictionary.Add(i.ToString(), i);

            dictionary.Add(5.ToString(), 5);
            dictionary.Add(6.ToString(), 6);

            string w = pixelImageData[0].ToString();// string.Empty;
            List<int> compressed = new List<int>();

            string returnMe = string.Empty;
            double codeSize = 3.0; // this is in bits

            foreach (int i in pixelImageData)
            {
                string wc = w + i;
                if (dictionary.ContainsKey(wc))
                {
                    w = wc;
                }
                else
                {

                    // write w to output                    
                    compressed.Add(dictionary[w]);

                    returnMe = reverse(Convert.ToString(((byte)dictionary[w]), 2).PadLeft((int)codeSize, '0')) + returnMe;
                    if (Math.Pow(2.0, codeSize) - 1 == dictionary.Count)
                    {
                        codeSize += 1.0;
                    }

                    // wc is a new sequence; add it to the dictionary
                    dictionary.Add(wc, dictionary.Count);
                    w = i.ToString();
                }
            }

            // write remaining output if necessary
            if (!string.IsNullOrEmpty(w))
                compressed.Add(dictionary[w]);

            return returnMe; //compressed;
        }

        static string reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        static byte[] GetBytes(string bitString)
        {
            byte[] result = Enumerable.Range(0, bitString.Length / 8).
                Select(pos => Convert.ToByte(
                    bitString.Substring(pos * 8, 8),
                    2)
                ).ToArray();

            List<byte> mahByteArray = new List<byte>();
            for (int i = result.Length - 1; i >= 0; i--)
            {
                mahByteArray.Add(result[i]);
            }

            return mahByteArray.ToArray();
        }
    }
}
