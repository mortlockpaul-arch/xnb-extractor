using System;
using System.Collections.Generic;
using System.Text;

using XnbExtractor.Xnb;

namespace XnbExtractor.Content;

public static class Texture2DReader
{
    public static XnbTexture Parse(XnbContentReader reader)
    {
        var texture = new XnbTexture();

        texture.Format = (SurfaceFormat)reader.ReadInt32();
        texture.Width = reader.ReadInt32();
        texture.Height = reader.ReadInt32();
        texture.MipCount = reader.ReadInt32();

        Console.WriteLine(
            $"Texture {texture.Width}x{texture.Height} {texture.Format} Mips:{texture.MipCount}"
        );

        for (int i = 0; i < texture.MipCount; i++)
        {
            int size = reader.ReadInt32();

            var data = reader.ReadBytes(size);

            texture.MipData.Add(data);

            Console.WriteLine(
                $"Mip {i}: {size} bytes"
            );
        }

        return texture;
    }
}