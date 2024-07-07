﻿using Avalonia.Controls.Platform;
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
    //See: https://giflib.sourceforge.net/whatsinagif/index.html
    public class GifHandler
    {
        /*
         * TODO:
         * - gif animations
         * - automatic pixel data
         * - spliting into subblocks with their own color table
         */

        int width, height;

        Stream stream;
        List<int[]> pictures;

        public GifHandler(int width, int height)
        {
            pictures = new List<int[]>();
            this.width = width;
            this.height = height;
        }

        public void AddPicture(int[] pixelData)
        {
            if (pixelData.Length != width * height)
                throw new Exception("Pixel data length is not correct");

            pictures.Add(pixelData);
        }

        public void Compile(string path)
        {
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
            writer.Write((byte)255);//w
            writer.Write((byte)255);
            writer.Write((byte)255);
            writer.Write((byte)255);//r
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);//b
            writer.Write((byte)0);
            writer.Write((byte)255);
            writer.Write((byte)0);//black
            writer.Write((byte)0);
            writer.Write((byte)0);

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
            //DEBUG: EXAMPLE DATA
            int[] pixelData = pictures[0];
            //getting lzw min code
            int max = pixelData.Max();
            int minCode = Convert.ToInt32(Math.Floor(Math.Log2(max)));

            if (minCode < 2)
                minCode = 2;

            var data = LZWCompression(pixelData, minCode);
            b = GetBytes(data, minCode);
            
            writer.Write(Convert.ToByte(minCode)); //lzw minimum code size
            writer.Write(Convert.ToByte(b.Length)); //number of bytes in sub-block
          
            for (int i = 0; i < b.Length; i++)
                writer.Write(b[i]);

            writer.Write(Convert.ToByte(0)); //without it everything is moved one pixel down
            writer.Write(Convert.ToByte(0x3b)); //End of GIF

            writer.Close();
            stream.Close();
        }

        /// <summary>
        /// can also return -1 that means that you have to increase code size
        /// </summary>
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

            bool nextAdd1 = false;

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


                    if (nextAdd1)
                    {
                        compressed.Add(-1);
                        nextAdd1 = false;
                        continue;
                    }
                        
                    if (Math.Pow(2, minCode + 1) - 1 == codes.Count-1)
                    {
                        nextAdd1 = true;

                        Debug.WriteLine(codes[^1]);

                        minCode++;
                    }
                }
            }

            compressed.Add(codes.IndexOf(buffer));
            compressed.Add(EOIcode);

            return compressed.ToArray();
        }

        byte[] GetBytes(int[] lzwData, int minCode)
        {
            string input = string.Empty;

            foreach(var d in lzwData)
            {
                if(d == -1)
                {
                    minCode++;
                    continue;
                }

                input = Convert.ToString(d, 2).PadLeft(minCode + 1, '0') + input;
            }

            if (input.Length % 8 != 0)
                input = input.PadLeft(((input.Length / 8 + 1) * 8) , '0');

            //convert string back to bytes
            int nBytes = input.Length / 8;
            var bytesAsStrings =
                Enumerable.Range(0, nBytes)
                          .Select(i => input.Substring(8 * i, 8)).Reverse();
            byte[] bytes = bytesAsStrings.Select(s => Convert.ToByte(s, 2)).ToArray();

            foreach (var b in bytes)
                Debug.WriteLine(Convert.ToString(b, 2));

            return bytes;
        }
    }
}
