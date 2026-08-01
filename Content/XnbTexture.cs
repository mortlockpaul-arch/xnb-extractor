using System;
using System.Collections.Generic;
using System.Text;

namespace XnbExtractor.Content;

public class XnbTexture
{
    public SurfaceFormat Format { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int MipCount { get; set; }

    public List<byte[]> MipData { get; set; } = new();

    public byte[] GetMip(int level = 0)
    {
        return MipData[level];
    }
}