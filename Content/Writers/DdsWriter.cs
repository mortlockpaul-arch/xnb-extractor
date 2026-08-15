using System;
using System.IO;
using System.Text;
using XnbExtractor.Content;
using static XnbExtractor.Utilities;
using static XnbExtractor.Utilities.TextureFormatDetector;

namespace XnbExtractor.Content.Writers
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
            path = Path.ChangeExtension(path, ".dds");
            if (texture.Format == TextureFormat.Dxt1)
            {
                int blockWidth = (texture.Width + 3) / 4;
                int blockHeight = (texture.Height + 3) / 4;

                int expectedDxt1 =
                    blockWidth *
                    blockHeight *
                    8;

                Console.WriteLine(
                    $"DXT1: {blockWidth}x{blockHeight} blocks, " +
                    $"{expectedDxt1} bytes");
            }
            Console.WriteLine($"Writing: {path}");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var stream = File.Create(path);
            using var writer = new BinaryWriter(stream);

            switch (texture.Format)
            {
                case TextureFormatDetector.TextureFormat.Alpha8:
                    WriteAlpha8Header(
                        writer,
                        texture.Width,
                        texture.Height
                    );

                    writer.Write(texture.MipData[0]);
                    break;


                case TextureFormatDetector.TextureFormat.Color:
                    {
                        WriteRgbaHeader(writer, texture);

                        byte[] rgba = ConvertXboxColorToRgba(
                            texture.MipData[0]);

                        writer.Write(rgba);
                        break;
                    }

                case TextureFormatDetector.TextureFormat.Rgba32:
                case TextureFormatDetector.TextureFormat.Rgb32:
                    WriteRgba32Header(
                        writer,
                        texture.Width,
                        texture.Height
                    );

                    writer.Write(texture.MipData[0]);
                    break;


                case TextureFormatDetector.TextureFormat.Bgr565:
                    WriteBgr565Header(writer, texture);
                    writer.Write(texture.MipData[0]);
                    break;

                case TextureFormatDetector.TextureFormat.Dxt1:
                case TextureFormatDetector.TextureFormat.Dxt3:
                case TextureFormatDetector.TextureFormat.Dxt5:
                    {
                        byte[] xboxData = texture.MipData[0];

                        byte[] endianData = SwapEndian16(xboxData);

                        byte[] dxtData = UntileXbox360Dxt(
                            endianData,
                            texture.Width,
                            texture.Height,
                            texture.Format);

                        Console.WriteLine(
                            $"{texture.Format}: {xboxData.Length} bytes -> " +
                            $"{endianData.Length} endian-swapped -> " +
                            $"{dxtData.Length} untiled");

                        WriteDxtHeader(
                            writer,
                            texture,
                            dxtData.Length);

                        writer.Write(dxtData);

                        break;
                    }
                default:
                    throw new NotSupportedException(
                        $"Unsupported texture format {texture.Format}");
            }
            Console.WriteLine(stream.Position);
        }


        private static byte[] SwapEndian16(byte[] data)
        {
            if ((data.Length & 1) != 0)
                throw new ArgumentException(
                    "Xbox texture data must have an even length.");

            byte[] result = new byte[data.Length];

            for (int i = 0; i < data.Length; i += 2)
            {
                result[i] = data[i + 1];
                result[i + 1] = data[i];
            }

            return result;
        }

        private static byte[] UntileXbox360Dxt( byte[] data, int width, int height, TextureFormatDetector.TextureFormat format)
        {
            int blockWidth = (width + 3) / 4;
            int blockHeight = (height + 3) / 4;

            int blockSize = format switch
            {
                TextureFormatDetector.TextureFormat.Dxt1 => 8,
                TextureFormatDetector.TextureFormat.Dxt3 => 16,
                TextureFormatDetector.TextureFormat.Dxt5 => 16,
                _ => throw new NotSupportedException(
                    $"Unsupported DXT format: {format}")
            };

            int expectedSize =
                blockWidth *
                blockHeight *
                blockSize;

            if (data.Length != expectedSize)
            {
                throw new InvalidDataException(
                    $"Invalid {format} size. " +
                    $"Expected {expectedSize}, got {data.Length}");
            }

            // TEMPORARY:
            // Return endian-corrected data unchanged.
            return data;
        }
        private static void WriteDxtHeader(
    BinaryWriter writer,
    XnbTexture texture,
    int dataLength)
        {
            writer.Write(Encoding.ASCII.GetBytes("DDS "));

            writer.Write(124);

            // DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH |
            // DDSD_PIXELFORMAT | DDSD_LINEARSIZE
            writer.Write(0x00081007);

            writer.Write(texture.Height);
            writer.Write(texture.Width);

            writer.Write(dataLength);

            writer.Write(0);
            writer.Write(texture.MipCount);

            for (int i = 0; i < 11; i++)
                writer.Write(0);

            // DDS_PIXELFORMAT
            writer.Write(32);
            writer.Write(0x4);
            writer.Write(FourCC(texture.Format switch
            {
                TextureFormatDetector.TextureFormat.Dxt1 => "DXT1",
                TextureFormatDetector.TextureFormat.Dxt3 => "DXT3",
                TextureFormatDetector.TextureFormat.Dxt5 => "DXT5",
                _ => throw new NotSupportedException(
                    $"Unsupported compressed format: {texture.Format}")
            }));

            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            // DDSCAPS
            writer.Write(0x1000);

            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
        }
        private static byte[] ConvertXboxColorToRgba(byte[] data)
        {
            byte[] output = new byte[data.Length];

            for (int i = 0; i < data.Length; i += 4)
            {
                output[i + 0] = data[i + 3]; // R
                output[i + 1] = data[i + 2]; // G
                output[i + 2] = data[i + 1]; // B
                output[i + 3] = data[i + 0]; // A
            }

            return output;
        }
        private static void WriteAlpha8Header(BinaryWriter writer, int width, int height)
        {
            // Magic
            writer.Write(Encoding.ASCII.GetBytes("DDS "));

            // DDS_HEADER
            writer.Write(124);

            // Flags
            writer.Write(
                0x0002100F
            ); // CAPS | HEIGHT | WIDTH | PITCH | PIXELFORMAT

            writer.Write(height);
            writer.Write(width);

            // Pitch for Alpha8 = bytes per row
            writer.Write(width);

            // Depth
            writer.Write(0);

            // MipMap count
            writer.Write(1);

            // Reserved 11
            for (int i = 0; i < 11; i++)
                writer.Write(0);


            // DDS_PIXELFORMAT
            writer.Write(32); // size

            writer.Write(
                0x00000002
            ); // DDPF_ALPHA

            writer.Write(0); // FourCC

            writer.Write(8); // RGBBitCount

            writer.Write(0); // R mask
            writer.Write(0); // G mask
            writer.Write(0); // B mask
            writer.Write(0x000000FF); // Alpha mask


            // DDS_CAPS
            writer.Write(0x1000);

            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
        }

        static byte[] ConvertRgb32(byte[] data)
        {
            byte[] output = new byte[data.Length];

            for (int i = 0; i < data.Length; i += 4)
            {
                output[i + 0] = data[i + 2]; // R
                output[i + 1] = data[i + 1]; // G
                output[i + 2] = data[i + 0]; // B
                output[i + 3] = data[i + 3]; // A
            }

            return output;
        }

        private static void WriteRgba32Header(BinaryWriter writer, int width, int height)
        {
            throw new NotImplementedException();
        }
        private static void WriteRgbaHeader(
    BinaryWriter writer,
    XnbTexture texture)
        {
            writer.Write(Encoding.ASCII.GetBytes("DDS "));

            // ---------------------------------------------------------
            // DDS_HEADER
            // ---------------------------------------------------------

            writer.Write(124);              // dwSize

            // DDSCAPS | DDSD_HEIGHT | DDSD_WIDTH |
            // DDSD_PITCH | DDSD_PIXELFORMAT
            writer.Write(0x0000100F);

            writer.Write(texture.Height);
            writer.Write(texture.Width);

            // Pitch
            writer.Write(texture.Width * 4);

            writer.Write(0);                // depth
            writer.Write(1);                // mipmap count

            // Reserved1[11]
            for (int i = 0; i < 11; i++)
                writer.Write(0);

            // ---------------------------------------------------------
            // DDS_PIXELFORMAT
            // ---------------------------------------------------------

            writer.Write(32);               // dwSize

            // DDPF_RGB | DDPF_ALPHAPIXELS
            writer.Write(0x41);

            writer.Write(0);                // dwFourCC

            writer.Write(32);               // dwRGBBitCount

            // Our data is RGBA byte order:
            //
            // byte 0 = R
            // byte 1 = G
            // byte 2 = B
            // byte 3 = A
            //
            writer.Write(0x000000FF);       // R
            writer.Write(0x0000FF00);       // G
            writer.Write(0x00FF0000);       // B
            writer.Write(0xFF000000);       // A

            // ---------------------------------------------------------
            // DDS_CAPS
            // ---------------------------------------------------------

            writer.Write(0x1000);            // DDSCAPS_TEXTURE
            writer.Write(0);                 // dwCaps2
            writer.Write(0);                 // dwCaps3
            writer.Write(0);                 // dwCaps4
            writer.Write(0);                 // reserved2
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
            writer.Write(texture.Width);
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
            Console.WriteLine($"DDS header position: {writer.BaseStream.Position}");
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