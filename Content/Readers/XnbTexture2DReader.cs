using System;
using System.Collections.Generic;
using XnbExtractor.Content;
using XnbExtractor.Content.Readers;
using XnbExtractor.Xnb;
using static XnbExtractor.Utilities;
using static XnbExtractor.Utilities.TextureFormatDetector;

public class XnbTexture2DReader : IXnbTypeReader
{
    public object Read(XnbContentReader reader)
    {
        Console.WriteLine("===== Texture2DReader =====");
        Console.WriteLine($"reader.Platform = {reader.Platform}");
        Console.WriteLine($"(int)reader.Platform = {(int)reader.Platform}");

        var texture = new XnbTexture();

        texture.RawFormat = reader.ReadInt32();

        texture.Format = TextureFormatDetector.Detect(
            reader.Platform,
            texture.RawFormat);

        Console.WriteLine($"Detected format = {texture.Format}");

        if (reader.Platform == TargetPlatform.Xbox360)
        {
            texture.XboxFormat =
                (XboxSurfaceFormat)texture.RawFormat;

            texture.WindowsFormat =
                TextureFormatDetector.ToWindowsFormat(
                    texture.Format);
        }
        else
        {
            texture.WindowsFormat =
                (SurfaceFormat)texture.RawFormat;
        }

        texture.Width = reader.ReadInt32();
        texture.Height = reader.ReadInt32();
        texture.MipCount = reader.ReadInt32();

        Console.WriteLine($"Raw Format     = {texture.RawFormat}");
        Console.WriteLine($"Xbox Format    = {texture.XboxFormat}");
        Console.WriteLine($"Windows Format = {texture.WindowsFormat}");
        Console.WriteLine($"Width          = {texture.Width}");
        Console.WriteLine($"Height         = {texture.Height}");
        Console.WriteLine($"MipCount       = {texture.MipCount}");

        long expected32 =
            (long)texture.Width * texture.Height * 4;

        long expected16 =
            (long)texture.Width * texture.Height * 2;

        long expected8 =
            (long)texture.Width * texture.Height;

        Console.WriteLine($"Expected 32-bit = {expected32}");
        Console.WriteLine($"Expected 16-bit = {expected16}");
        Console.WriteLine($"Expected 8-bit  = {expected8}");
        for (int mip = 0; mip < texture.MipCount; mip++)
        {
            int mipWidth = Math.Max(1, texture.Width >> mip);
            int mipHeight = Math.Max(1, texture.Height >> mip);

            int storedSize = reader.ReadInt32();

            Console.WriteLine(
                $"Mip {mip}: {mipWidth}x{mipHeight}, " +
                $"stored size = {storedSize}");

            byte[] data = reader.ReadBytes(storedSize);
            for (int i = 0; i < Math.Min(8, data.Length / 4); i++)
            {
                uint value = BitConverter.ToUInt32(data, i * 4);

                Console.WriteLine(
                    $"DWORD[{i}] = 0x{value:X8}");
            }
            Console.WriteLine(
                $"Mip {mip}: actually read {data.Length} bytes");

            Console.WriteLine(
                $"First 32 bytes: {BitConverter.ToString(data, 0, Math.Min(32, data.Length))}");

            texture.MipData.Add(data);
        }
        Console.WriteLine(
    $"MipData.Count = {texture.MipData.Count}");

        if (texture.MipData.Count > 0)
        {
            Console.WriteLine(
                $"Actual = {texture.MipData[0].Length}");
        }
        return texture;
    }
}