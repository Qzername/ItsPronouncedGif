using Avalonia.Media;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;


namespace ItsPronouncedGif
{
    //See: https://giflib.sourceforge.net/whatsinagif/index.html
    //also: https://www.matthewflickinger.com/lab/whatsinagif/
    public class GifCreator
    {
        int width, height;

        Stream stream;
        List<int[]> pictures;

        List<Color> gct;

        public GifCreator(int width, int height)
        {
            pictures = new List<int[]>();
            this.width = width;
            this.height = height;
        }

        public void AddPicture(Color[,] picture)
        {
            if (gct is null)
                gct = new List<Color>();

            int[] pixelData = new int[picture.Length];

            //temp solution
            for (int y = 0; y < picture.GetLength(1);y++)
            {
                string keys = string.Empty;

                for (int x = 0; x < picture.GetLength(0); x++)
                {
                    var color = picture[x, y];

                    if (gct.Contains(color))
                    {
                        pixelData[y * width + x] = gct.IndexOf(color);
                    }
                    else if (gct.Count == 256)
                        pixelData[y * width + x] = 0;
                    else
                    {
                        pixelData[y * width + x] = gct.Count;
                        gct.Add(color);
                    }

                    keys += gct[pixelData[y * width + x]] + " ";
                }
            }
                
            pictures.Add(pixelData);
        }

        public void Compile(string path)
        {
            while (gct.Count < 256)
                gct.Add(Color.FromRgb(0, 0, 0));

            stream = new FileStream(path, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(stream);

            //Header - version of gif
            writer.Write(new char[3] { 'G', 'I', 'F' });
            writer.Write(new char[3] { '8', '9', 'a' });

            // --- LSD ---

            //width and height  
            writer.Write((short)width);
            writer.Write((short)height);

            //Color table information
            BitArray cti = new BitArray(new byte[1]);

            //Color table requirement
            cti[0] = true; 
            cti[1] = true;
            cti[2] = true;

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

            // --- GCT --- 
            foreach(var c in gct)
            { 
                writer.Write(c.R);
                writer.Write(c.G);
                writer.Write(c.B);
            }

            // --- Application Extension --- 
            writer.Write((byte)0x21); //extension introducer
            writer.Write((byte)0xFF);
            writer.Write((byte)0x0B); //11 bytes comming
            writer.Write(new char[8] { 'N', 'E', 'T', 'S', 'C', 'A', 'P', 'E' });
            writer.Write(new char[3] { '2', '.', '0' });
            writer.Write((byte)0x03);
            writer.Write((byte)0x01);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);

            // --- Pictures ---
            for (int i = 0; i < pictures.Count;i++)
            {
                // --- GCE ---
                writer.Write((byte)0x21); //extension introducer
                writer.Write((byte)0xF9); // GCL
                writer.Write((byte)4); // 4 bytes comming

                BitArray packedField = new BitArray(new byte[1]);

                //Transparent color flag
                packedField[0] = false;

                //User input flag
                packedField[1] = false;

                //disposal method
                packedField[2] = true;
                packedField[3] = false;
                packedField[4] = false;

                //not used
                packedField[5] = false;
                packedField[6] = false;
                packedField[7] = false;

                b = new byte[1];
                packedField.CopyTo(b, 0);
                writer.Write(b);

                writer.Write((short)20); //delay time
                writer.Write((byte)0); //transparent color index
                writer.Write((byte)0); //block terminator

                // --- Picture --- 
                //Picture descriptor
                writer.Write(Convert.ToByte(0x2c)); //img separator character
                writer.Write(Convert.ToInt16(0)); //img left position
                writer.Write(Convert.ToInt16(0)); //img top positon                    
                writer.Write(Convert.ToInt16(width)); //img width                    
                writer.Write(Convert.ToInt16(height)); //img height

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
                int[] pixelData = pictures[i];
                //getting lzw min code
                int max = pixelData.Max();

                if (max == 0)
                    max = 1;

                int minCode = Convert.ToInt32(Math.Ceiling(Math.Log2(max+1)));

                if (minCode < 2)
                    minCode = 2;

                var data = LZWCompression(pixelData, minCode);
                b = GetBytes(data, minCode);

                writer.Write(Convert.ToByte(minCode)); //lzw minimum code size

                int bytesRemaining = b.Length;

                for (int a = 0; a < Math.Ceiling(b.Length / 255f); a++)
                {
                    var currentBlockLength = bytesRemaining > 255 ? 255 : bytesRemaining;

                    writer.Write((byte)currentBlockLength); //number of bytes in sub-block

                    for (int Bi = 0; Bi < currentBlockLength; Bi++)
                        writer.Write(b[Bi + a*255]);

                    bytesRemaining -= 255;
                }

                writer.Write(Convert.ToByte(0)); //0 bytes comming
            }

            writer.Write(Convert.ToByte(0x3b)); //End of GIF - block terminator

            writer.Close();
            stream.Close();
        }

        /// <summary>
        /// can also return -1 that means that you have to increase code size
        /// </summary>
        int[] LZWCompression(int[] data, int minCode)
        {
            int oldMinCode = minCode;

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

            bool nextAdd1 = false;
            bool overflow = false;

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

                    if (codes.Count == 4096) //limit for the mincode
                    {
                        overflow = true;

                        compressed.Add(clearCode);

                        List<int> tempData = new List<int>();

                        for (int x = i; x < data.Length; x++)
                            tempData.Add(data[x]);

                        compressed.AddRange(LZWCompression(tempData.ToArray(), oldMinCode));
                        compressed.RemoveAt(compressed.Count - 1);
                        break;
                    }

                    if (nextAdd1)
                    {
                        compressed.Add(-1);
                        nextAdd1 = false;
                        continue;
                    }

                    if (Math.Pow(2, minCode + 1) - 1 == codes.Count-1)
                    {
                        nextAdd1 = true;
                        minCode++;
                    }
                }

            }

            if(!overflow)
                compressed.Add(codes.IndexOf(buffer));
            compressed.Add(EOIcode);

            return compressed.ToArray();
        }

        byte[] GetBytes(int[] lzwData, int minCode)
        {
            string input = string.Empty;

            int clearCode = (int)Math.Pow(2, minCode);

            int oldMinCode = minCode;

            foreach(var d in lzwData)
            {
                if(d == -1)
                {
                    minCode++;
                    continue;
                }

                input = Convert.ToString(d, 2).PadLeft(minCode + 1, '0') + input;

                if (d == clearCode)
                    minCode = oldMinCode;
            }

            if (input.Length % 8 != 0)
                input = input.PadLeft(((input.Length / 8 + 1) * 8) , '0');

            //convert string back to bytes
            int nBytes = input.Length / 8;
            var bytesAsStrings =
                Enumerable.Range(0, nBytes)
                          .Select(i => input.Substring(8 * i, 8)).Reverse();
            byte[] bytes = bytesAsStrings.Select(s => Convert.ToByte(s, 2)).ToArray();

            return bytes;
        }
    }
}
