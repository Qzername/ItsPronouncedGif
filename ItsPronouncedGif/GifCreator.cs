using Avalonia.Media;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using SD = System.Drawing;
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
        List<PictureData> pictures;

        List<Color> gct;

        public GifCreator(int width, int height)
        {
            pictures = new List<PictureData>();
            this.width = width;
            this.height = height;

            /*
             * create static gct
             * it is not efficient, but for time of writing this comment,
             * im foccusing on increasing performance of screen capture
             */
            gct = new List<Color>();

            byte interval = 256 / 6;

            for (byte x = 0; x < 6; x++)
                for (byte y = 0; y < 6; y++)
                    for (byte z = 0; z < 6; z++)
                        gct.Add(new Color(255, (byte)(x * interval),
                                               (byte)(y * interval),
                                               (byte)(z * interval)));

            for (byte i = 0; i < 40; i++)
                gct.Add(new Color(255, 0, 0, 0));
        }

        public void AddPicture(Color[,] picture, int delay)
        {
            int[] pixelData = new int[picture.Length*3];

            byte interval = 256 / 5;

            for (int y = 0; y < picture.GetLength(1); y++)
            {
                for (int x = 0; x < picture.GetLength(0); x++)
                {
                    int index = (y * width + x) * 3;
                    Color currentColor = picture[x, y];

                    pixelData[index] = currentColor.R;
                    pixelData[index+1] = currentColor.G;
                    pixelData[index+2] = currentColor.B;
                }
            }

            pictures.Add(new PictureData()
            {
                Delay = delay,
                PixelData = pixelData,
            });
        }

        public void Compile(string path)
        {
            while (gct.Count < 256)
                gct.Add(Color.FromRgb(0, 0, 0));

            stream = new FileStream(path, FileMode.Create);
            BinaryWriter fileWriter = new BinaryWriter(stream);

            StartFile(fileWriter);

            // --- Pictures ---
            for (int i = 0; i < pictures.Count; i++)
            {
                var currentPicture = pictures[i];
                CompilePicture(fileWriter, currentPicture);
            }

            FinishFile(fileWriter);

            stream.Close();
        }

        void StartFile(BinaryWriter fileWriter)
        {
            //Header - version of gif
            fileWriter.Write(new char[3] { 'G', 'I', 'F' });
            fileWriter.Write(new char[3] { '8', '9', 'a' });

            // --- LSD ---

            //width and height  
            fileWriter.Write((short)width);
            fileWriter.Write((short)height);

            //Color table information
            BitArray cti = new BitArray(new byte[1]);

            //global color table size
            cti[0] = false;
            cti[1] = false;
            cti[2] = false;

            //Is sorted
            cti[3] = false;

            //Bits per sample 
            cti[4] = true;
            cti[5] = false;
            cti[6] = false;

            //Global Color Table
            cti[7] = false;

            byte[] b = new byte[1];
            cti.CopyTo(b, 0);
            fileWriter.Write(b);

            //Background color index
            fileWriter.Write(Convert.ToByte(0));

            //Pixel aspect ratio
            fileWriter.Write(Convert.ToByte(0));

            // --- Application Extension --- 
            fileWriter.Write((byte)0x21); //extension introducer
            fileWriter.Write((byte)0xFF);
            fileWriter.Write((byte)0x0B); //11 bytes comming
            fileWriter.Write(['N', 'E', 'T', 'S', 'C', 'A', 'P', 'E']);
            fileWriter.Write(['2', '.', '0']);
            fileWriter.Write((byte)0x03);
            fileWriter.Write((byte)0x01);
            fileWriter.Write((byte)0);
            fileWriter.Write((byte)0);
            fileWriter.Write((byte)0);
        }

        void CompilePicture(BinaryWriter fileWriter, PictureData pictureData)
        {
            // --- GCE ---
            fileWriter.Write((byte)0x21); //extension introducer
            fileWriter.Write((byte)0xF9); // GCL
            fileWriter.Write((byte)4); // 4 bytes comming

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

            byte[] b = new byte[1];
            packedField.CopyTo(b, 0);
            fileWriter.Write(b);

            fileWriter.Write((ushort)pictureData.Delay / 10); //delay time (in 0,01s)
            /*writer.Write((byte)0); //transparent color index
              writer.Write((byte)0); //block terminator*/

            // --- Picture --- 
            //Picture descriptor
            fileWriter.Write(Convert.ToByte(0x2c)); //img separator character
            fileWriter.Write(Convert.ToInt16(0)); //img left position
            fileWriter.Write(Convert.ToInt16(0)); //img top positon                    
            fileWriter.Write(Convert.ToInt16(width)); //img width                    
            fileWriter.Write(Convert.ToInt16(height)); //img height

            var desc = new BitArray(new byte[1]);

            // size of local color table
            desc[0] = true; 
            desc[1] = true;
            desc[2] = true; 

            //reserved for future use
            desc[3] = false;
            desc[4] = false;

            // sort flag
            desc[5] = false;

            // img not interlaced
            desc[6] = false;

            // local color table present
            desc[7] = true;

            b = new byte[1];
            desc.CopyTo(b, 0);
            fileWriter.Write(b);

            //Picture data
            int[] pixelData = pictureData.PixelData;

            OctreeQuantizatior octreeQuantization = new OctreeQuantizatior();

            for (int i = 0; i < pixelData.Length; i+=3)
            {
                octreeQuantization.AddColor(pixelData[i],
                                            pixelData[i + 1],
                                            pixelData[i + 2]);
            }

            octreeQuantization.GetColor();
            pixelData = octreeQuantization.Quintize(pixelData, 3);

            var paletteColors = octreeQuantization.Palette.Keys.ToArray();

            //local color table
            for (int i = 0; i < paletteColors.Length && i < 256; i++)
            {
                var c = paletteColors[i];

                fileWriter.Write((byte)c.R);
                fileWriter.Write((byte)c.G);
                fileWriter.Write((byte)c.B);
            };

            //fill remaning colors
            if (paletteColors.Length < 256)
            {
                for (int i = paletteColors.Length; i < 256; i++)
                {
                    fileWriter.Write((byte)0);
                    fileWriter.Write((byte)0);
                    fileWriter.Write((byte)0);
                }
            }

            //get indexes
            int[] indexedPixelData = new int[pixelData.Length / 3];

            for (int i = 0; i < pixelData.Length; i += 3)
            {
                var colorKey = new ColorKey(pixelData[i], pixelData[i + 1], pixelData[i + 2]);

                if (octreeQuantization.Palette.ContainsKey(colorKey))
                    indexedPixelData[i / 3] = octreeQuantization.Palette[colorKey];
                else  //fix quantizator so this is no longer needed
                    indexedPixelData[i / 3] = 255;
            }

            //getting lzw min code
            int max = pixelData.Max();

            if (max == 0)
                max = 1;

            int minCode = Convert.ToInt32(Math.Ceiling(Math.Log2(max + 1)));

            if (minCode < 2)
                minCode = 2;

            var aa = DateTime.Now;
            var data = LZWCompression(indexedPixelData, minCode);

            var bb = DateTime.Now;

            b = GetBytes(data, minCode);
            var cc = DateTime.Now;

            fileWriter.Write(Convert.ToByte(minCode)); //lzw minimum code size

            int bytesRemaining = b.Length;

            for (int a = 0; a < Math.Ceiling(b.Length / 255f); a++)
            {
                var currentBlockLength = bytesRemaining > 255 ? 255 : bytesRemaining;

                fileWriter.Write((byte)currentBlockLength); //number of bytes in sub-block

                for (int Bi = 0; Bi < currentBlockLength; Bi++)
                    fileWriter.Write(b[Bi + a * 255]);

                bytesRemaining -= 255;
            }

            fileWriter.Write(Convert.ToByte(0)); //0 bytes comming
        }

        void FinishFile(BinaryWriter fileWriter)
        {
            fileWriter.Write(Convert.ToByte(0x3b)); //End of GIF - block terminator

            fileWriter.Close();
        }

        /// <summary>
        /// can also return -1 that means that you have to increase code size
        /// </summary>
        int[] LZWCompression(int[] data, int minCode)
        {
            int oldMinCode = minCode;

            Dictionary<string, int> codesHashMap = new Dictionary<string, int>();
            List<int> compressed = new List<int>();

            int colorsAmount = Convert.ToInt32(Math.Pow(2, minCode));

            int clearCode = colorsAmount;
            int EOIcode = colorsAmount + 1;

            //filling color codes wtih additional codes
            for (int i = 0; i < colorsAmount + 2; i++)
                codesHashMap[i.ToString()] = i;

            //send clear code
            compressed.Add(clearCode);

            string buffer = data[0].ToString();

            bool nextAdd1 = false;
            bool overflow = false;

            for (int i = 1; i < data.Length; i++)
            {
                var k = data[i];
                string strK = $" {k}";

                if (codesHashMap.ContainsKey(buffer + strK))
                    buffer += strK;
                else
                {
                    codesHashMap[buffer + strK] = codesHashMap.Count;

                    compressed.Add(codesHashMap[buffer]);

                    buffer = k.ToString();

                    if (codesHashMap.Count == 4096) //limit for the mincode
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

                    if (Math.Pow(2, minCode + 1) - 1 == codesHashMap.Count - 1)
                    {
                        nextAdd1 = true;
                        minCode++;
                    }
                }

            }

            if (!overflow)
                compressed.Add(codesHashMap[buffer]);

            compressed.Add(EOIcode);

            return compressed.ToArray();
        }

        byte[] GetBytes(int[] lzwData, int minCode)
        {
            List<byte> output = new List<byte>();
            int bitBuffer = 0;
            int bitCount = 0;

            int clearCode = 1 << minCode;
            int currentCodeSize = minCode + 1;
            int maxCodeSize = 12; // per LZW spec

            int codeSizeLimit = 1 << currentCodeSize;
            int oldMinCode = minCode;

            foreach (var code in lzwData)
            {
                if (code == -1)
                {
                    currentCodeSize++;
                    if (currentCodeSize > maxCodeSize)
                        currentCodeSize = maxCodeSize;

                    codeSizeLimit = 1 << currentCodeSize;
                    continue;
                }

                // Add bits of the current code to buffer
                bitBuffer |= code << bitCount;
                bitCount += currentCodeSize;

                // Flush full bytes from buffer
                while (bitCount >= 8)
                {
                    output.Add((byte)(bitBuffer & 0xFF));
                    bitBuffer >>= 8;
                    bitCount -= 8;
                }

                if (code == clearCode)
                {
                    currentCodeSize = minCode + 1;
                    codeSizeLimit = 1 << currentCodeSize;
                }
            }

            // Flush remaining bits
            if (bitCount > 0)
                output.Add((byte)(bitBuffer & 0xFF));

            return output.ToArray();
        }
    }

        struct PictureData
    {
        public int Delay;
        public int[] PixelData;
    }
}
