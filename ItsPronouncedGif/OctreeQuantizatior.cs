using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace ItsPronouncedGif
{

    /*
     * this is poorly written yet working octree quantization
     * i will probably improve it in the future
     * 
     * for now i wanted to focus on improving color handling as soon as possible
     */

    public class OctreeQuantizatior
    {
        Node root;

        public Dictionary<ColorKey, int> Palette { get; private set; }

        public OctreeQuantizatior()
        {
            root = new Node();
        }

        public void AddColor(int r, int g, int b)
        {
            root.AddColor(r, g, b, 0);
        }

        public int[] GetColor()
        {
            return root.Optimize(0);
        }

        public int[] Quintize(int[] photo, int level)
        {
            Palette = new Dictionary<ColorKey, int>();

            int[] result = new int[photo.Length];

            Debug.WriteLine(photo[0] + " " + photo[1] + " " + photo[2]);

            for (int i = 0; i < photo.Length; i += 3)
            {
                int r = photo[i];
                int g = photo[i + 1];
                int b = photo[i + 2];
                var color = root.GetColor((byte)r, (byte)g, (byte)b, 0, level);
                var colorKey = new ColorKey(color[0], color[1], color[2]); 

                if (!Palette.ContainsKey(colorKey))
                {
                    Palette[colorKey] = Palette.Count;
                }

                result[i] = color[0];
                result[i + 1] = color[1];
                result[i + 2] = color[2];
            }

            return result;
        }
    }

    class Node
    {
        int r, g, b;
        int pixelCount;

        Node[] children;

        public Node()
        {
            children = new Node[8];
        }

        public void AddColor(int r, int g, int b, int level)
        {
            //max level, add color to this node
            if (level == 8)
            {
                this.r = r;
                this.g = g;
                this.b = b;

                pixelCount++;
                return;
            }

            //if not max level, go to the child node
            int index = GetIndex(r, g, b, level);

            if (children[index] == null)
                children[index] = new Node();

            children[index].AddColor(r, g, b, level + 1);
        }

        public int[] Optimize(int level)
        {
            if (level == 8)
                return [r, g, b, pixelCount];

            int[] totalColor = new int[3];
            int totalPixelCount = 0;

            foreach (var node in children)
            {
                if (node is null)
                    continue;

                var color = node.Optimize(level + 1);
                totalColor[0] += color[0] * color[3];
                totalColor[1] += color[1] * color[3];
                totalColor[2] += color[2] * color[3];
                totalPixelCount += color[3];
            }

            r = totalColor[0] / totalPixelCount;
            g = totalColor[1] / totalPixelCount;
            b = totalColor[2] / totalPixelCount;
            pixelCount = totalPixelCount;

            return [r, g, b, totalPixelCount];
        }

        public int[] GetColor(byte r, byte g, byte b, int level, int maxLevel)
        {
            if (level == maxLevel || pixelCount < 50)
            {
                return [this.r, this.g, this.b];
            }

            int index = GetIndex(r, g, b, level);
            return children[index].GetColor(r, g, b, level + 1, maxLevel);
        }
        int GetIndex(int r, int g, int b, int level)
        {
            byte final = 0;

            var mask = Convert.ToByte(0b10000000 >> level);

            if ((r & mask) != 0)
                final |= 0b100;

            if ((g & mask) != 0)
                final |= 0b010;

            if ((b & mask) != 0)
                final |= 0b001;

            return final;
        }

        /*
    int GetIndex(int r, int g, int b, int level) =>
        GetBit(r, level) * 4 + 
        GetBit(g, level) * 2 + 
        GetBit(b, level);

    int GetBit(int value, int level) => Convert.ToInt32((value & (1 << level)) != 0);*/
    }

    public struct ColorKey
    {
        public int R;
        public int G;
        public int B;

        public ColorKey(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }
    }
}