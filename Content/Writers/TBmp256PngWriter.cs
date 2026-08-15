using System.Drawing;
using System.Drawing.Imaging;

namespace XnbExtractor.Content.Writers
{
    public static class TBmp256PngWriter
    {
        public static void Write(
            int width,
            int height,
            byte[] palIdx,
            uint[] palette,
            string outputFile)
        {
            using Bitmap image = new Bitmap(
                width,
                height,
                PixelFormat.Format32bppArgb);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = y * width + x;
                    byte index = palIdx[offset];
                    uint packed = palette[index];

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
}