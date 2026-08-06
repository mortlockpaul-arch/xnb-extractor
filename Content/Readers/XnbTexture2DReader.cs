using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using XnbExtractor.Xnb;

namespace XnbExtractor.Content;

public static class XnbTexture2DReader
{
    public static XnbTexture Parse(XnbContentReader reader)
    {
        Console.WriteLine("===== Texture2DReader =====");
        Console.WriteLine($"reader.Platform = {reader.Platform}");
        Console.WriteLine($"(int)reader.Platform = {(int)reader.Platform}");
        Console.WriteLine($"Xbox360 enum value = {(int)TargetPlatform.Xbox360}");
        Console.WriteLine($"Windows enum value = {(int)TargetPlatform.Windows}");
        var texture = new XnbTexture();
        Console.WriteLine($"Reader.Platform = {reader.Platform}");
        texture.RawFormat = reader.ReadInt32();

        if (reader.Platform == TargetPlatform.Xbox360)
        {
            texture.XboxFormat = (XboxSurfaceFormat)texture.RawFormat;
        }
        else
        {
            texture.WindowsFormat = (SurfaceFormat)texture.RawFormat;
        }
        //texture.Format = (SurfaceFormat)reader.ReadInt32();
        texture.Width = reader.ReadInt32();
        texture.Height = reader.ReadInt32();
        texture.MipCount = reader.ReadInt32();

        Console.WriteLine($"Position = {reader.Position}");

        long pos = reader.Position;

        byte[] peek = reader.ReadBytes(16);

        Console.Write("Next bytes: ");
        foreach (byte b in peek)
            Console.Write($"{b:X2} ");
        Console.WriteLine();

        reader.Seek(pos);

        Console.WriteLine($"Raw Format = {texture.RawFormat}");
        Console.WriteLine($"Raw Format = {texture.RawFormat} (0x{texture.RawFormat:X8})");
        if (reader.Platform == TargetPlatform.Xbox360)
            Console.WriteLine($"Xbox Format = {texture.XboxFormat}");
        else
            Console.WriteLine($"Windows Format = {texture.WindowsFormat}");

        Console.WriteLine($"Width={texture.Width}");
        Console.WriteLine($"Height={texture.Height}");
        Console.WriteLine($"MipCount={texture.MipCount}");


        for (int mip = 0; mip < texture.MipCount; mip++)
        {
            int size = reader.ReadInt32();
            Console.WriteLine($"Stored mip size = {size}");

            var data = reader.ReadBytes(size);
            texture.MipData.Add(data);

            Console.WriteLine("First 8 pixels:");

            int count = Math.Min(8, data.Length / 4);

            for (int p = 0; p < count; p++)
            {
                uint pixel = BitConverter.ToUInt32(data, p * 4);
                Console.WriteLine($"{p}: 0x{pixel:X8}");
            }

            Console.WriteLine($"Mip {mip}: {size} bytes");
        }

        return texture;
    }
}