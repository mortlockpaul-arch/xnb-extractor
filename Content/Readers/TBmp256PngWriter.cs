using System;
using System.Drawing;
using XnbExtractor.Content;
using XnbExtractor.Models;

namespace XnbExtractor.Content.Readers
{
    using System.Drawing;
    using System.Drawing.Imaging;

    public static class TBmp256PngWriter
    {
        public static void Write(
            XnbTBmp256 bmp,
            uint[] palette,
            string outputFile)
        {
            using Bitmap image = new Bitmap(
                bmp.Width,
                bmp.Height,
                PixelFormat.Format32bppArgb);

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    int offset = y * bmp.Width + x;

                    byte index = bmp.PalIdx[offset];

                    uint packed = palette[index];

                    // XNA Color.PackedValue layout:
                    byte r = (byte)(packed & 0xFF);
                    byte g = (byte)((packed >> 8) & 0xFF);
                    byte b = (byte)((packed >> 16) & 0xFF);
                    byte a = (byte)((packed >> 24) & 0xFF);

                    image.SetPixel(
                        x,
                        y,
                        Color.FromArgb(a, r, g, b));
                }
            }

            image.Save(outputFile, ImageFormat.Png);
        }
    }
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