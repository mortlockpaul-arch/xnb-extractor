using System;
using XnbExtractor.Models;

namespace XnbExtractor.Content.Readers
{
    using System.Drawing;

    public class TBmp256Reader : IXnbTypeReader
    {
        public static Bitmap Render(XnbTBmp256 bmp, uint[] palette)
        {
            Bitmap image = new(bmp.Width, bmp.Height);

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    byte index = bmp.PalIdx[y * bmp.Width + x];

                    uint packed = palette[index];

                    // convert packed uint to Color
                }
            }

            return image;
        }
        public object Read(XnbContentReader reader)
        {
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();

            byte[] palIdx = reader.ReadBytes(width * height);

            Console.WriteLine($"Width  : {width}");
            Console.WriteLine($"Height : {height}");
            Console.WriteLine($"Pixels : {palIdx.Length}");

            return new XnbTBmp256(width, height, palIdx);
        }
    }


}