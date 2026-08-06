using System;
using System.IO;
using System.Text;
using XnbExtractor.Content;

namespace XnbExtractor.Writers
{
    public static class DdsWriter
    {
        public static void Write(XnbTexture texture, string path)
        {
            Console.WriteLine($"Raw Format: {texture.RawFormat}");
            Console.WriteLine($"Raw bytes: {texture.MipData[0].Length}");
            int expected;

            if (texture.XboxFormat == XboxSurfaceFormat.Color)
                expected = texture.Width * texture.Height * 4;
            else if (texture.XboxFormat == XboxSurfaceFormat.Bgr565)
                expected = texture.Width * texture.Height * 2;
            else
                expected = texture.MipData[0].Length;

            Console.WriteLine($"Expected: {expected}");

            int bytesPerPixel = texture.MipData[0].Length / (texture.Width * texture.Height);

            Console.WriteLine($"BytesPerPixel = {bytesPerPixel}");

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var stream = File.Create(path);
            using var writer = new BinaryWriter(stream);

            if (texture.XboxFormat == XboxSurfaceFormat.Color)
            {
                WriteRgbaHeader(writer, texture);
                writer.Write(texture.MipData[0]);
                return;
            }

            if (texture.XboxFormat == XboxSurfaceFormat.Dxt1)
            {
                WriteDxt1Header(writer, texture);
                writer.Write(texture.MipData[0]);
                return;
            }
            if (texture.XboxFormat.HasValue)
            {
                switch (texture.XboxFormat.Value)
                {
                    case XboxSurfaceFormat.Color:
                        WriteRgbaHeader(writer, texture);
                        writer.Write(texture.MipData[0]);
                        break;

                    case XboxSurfaceFormat.Bgr565:
                        WriteBgr565Header(writer, texture);
                        writer.Write(texture.MipData[0]);
                        break;

                    case XboxSurfaceFormat.Dxt1:
                        WriteDxt1Header(writer, texture);
                        writer.Write(texture.MipData[0]);
                        break;

                    default:
                        throw new NotSupportedException(
                            $"Unsupported Xbox format {texture.XboxFormat}");
                }
            }
            else
            {
                if (texture.WindowsFormat.HasValue)
                {
                    switch (texture.WindowsFormat.Value)
                    {
                        case SurfaceFormat.Dxt1:
                            WriteDxt1Header(writer, texture);

                            foreach (var dxtMip in texture.MipData)
                            {
                                writer.Write(dxtMip);
                            }
                            break;

                        case SurfaceFormat.Bgr565:
                            {
                                WriteRgbaHeader(writer, texture);

                                var mip = texture.MipData[0];

                                Console.WriteLine($"Raw length = {mip.Length}");

                                for (int i = 0; i < 64; i += 16)
                                {
                                    Console.Write($"{i:X4}: ");

                                    for (int j = 0; j < 16; j++)
                                        Console.Write($"{mip[i + j]:X2} ");

                                    Console.WriteLine();
                                }

                                // Test 1: treat it as RGBA8888
                                writer.Write(mip);

                                break;
                            }

                        default:
                            throw new NotSupportedException(
                                $"DDS format not supported: {texture.WindowsFormat}");
                    }
                    Console.WriteLine(stream.Position);
                }
            }
        }

        private static void WriteRgbaHeader(BinaryWriter writer, XnbTexture texture)
        {
            writer.Write(Encoding.ASCII.GetBytes("DDS "));

            writer.Write(124);

            // CAPS | HEIGHT | WIDTH | PITCH | PIXELFORMAT
            writer.Write(0x0000100F);

            writer.Write(texture.Height);
            writer.Write(texture.Width);
            writer.Write(texture.Width * 4); // RGBA pitch
            writer.Write(0);
            writer.Write(1);

            for (int i = 0; i < 11; i++)
                writer.Write(0);

            // Pixel format
            writer.Write(32);
            writer.Write(0x41); // DDPF_RGB | DDPF_ALPHAPIXELS
            writer.Write(0);
            writer.Write(32);

            writer.Write(0x000000FF); // R
            writer.Write(0x0000FF00); // G
            writer.Write(0x00FF0000); // B
            writer.Write(unchecked((int)0xFF000000)); // A

            // Caps
            writer.Write(0x1000);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
        }
        private static byte[] ConvertBgr565ToRgba(byte[] data, int width, int height)
        {
            byte[] output = new byte[width * height * 4];

            for (int i = 0; i < width * height; i++)
            {
                ushort pixel = BitConverter.ToUInt16(data, i * 2);

                byte r = (byte)(((pixel >> 11) & 0x1F) << 3);
                byte g = (byte)(((pixel >> 5) & 0x3F) << 2);
                byte b = (byte)((pixel & 0x1F) << 3);

                output[i * 4 + 0] = r;
                output[i * 4 + 1] = g;
                output[i * 4 + 2] = b;
                output[i * 4 + 3] = 255;
            }

            return output;
        }

        private static void WriteBgr565Header(BinaryWriter writer, XnbTexture texture)
        {
            int pitch = texture.Width * 2;

            // Magic
            writer.Write(Encoding.ASCII.GetBytes("DDS "));

            // DDS_HEADER
            writer.Write(124);              // dwSize
            writer.Write(0x0000100F);       // flags: CAPS | HEIGHT | WIDTH | PIXELFORMAT | PITCH
            writer.Write(texture.Height);
            writer.Write(pitch);
            writer.Write(0);                // depth
            writer.Write(texture.MipCount);

            // reserved
            for (int i = 0; i < 11; i++)
                writer.Write(0);

            // DDS_PIXELFORMAT
            writer.Write(32);               // size
            writer.Write(0x40);             // DDPF_RGB
            writer.Write(0);                // fourCC
            writer.Write(16);               // RGB bit count

            writer.Write(0xF800);           // R
            writer.Write(0x07E0);           // G
            writer.Write(0x001F);           // B
            writer.Write(0);                // A

            // DDS_CAPS
            writer.Write(0x1000);   // DDSCAPS_TEXTURE
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);        // reserved2
            Console.WriteLine($"DDS header position: {writer.BaseStream.Position}");
        }

        private static void WriteDxt1Header(BinaryWriter writer, XnbTexture texture)
        {
            writer.Write(Encoding.ASCII.GetBytes("DDS "));

            // DDS_HEADER
            writer.Write(124);

            // DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH |
            // DDSD_PIXELFORMAT | DDSD_LINEARSIZE
            writer.Write(0x00081007);

            writer.Write(texture.Height);
            writer.Write(texture.Width);

            // Linear size
            writer.Write(texture.MipData[0].Length);

            writer.Write(0); // depth
            writer.Write(texture.MipCount);

            for (int i = 0; i < 11; i++)
                writer.Write(0);


            // DDS_PIXELFORMAT
            writer.Write(32);        // size
            writer.Write(0x4);       // DDPF_FOURCC
            writer.Write(FourCC("DXT1"));

            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);


            // DDSCAPS
            writer.Write(0x1000);    // DDSCAPS_TEXTURE

            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0); // reserved2
        }


        private static int GetFourCC(SurfaceFormat format)
        {
            return format switch
            {
                SurfaceFormat.Dxt1 => FourCC("DXT1"),
                SurfaceFormat.Dxt3 => FourCC("DXT3"),
                SurfaceFormat.Dxt5 => FourCC("DXT5"),

                _ => throw new NotSupportedException(
                    $"DDS format not supported: {format}"
                )
            };
        }


        private static int FourCC(string value)
        {
            return value[0]
                 | value[1] << 8
                 | value[2] << 16
                 | value[3] << 24;
        }
    }
}