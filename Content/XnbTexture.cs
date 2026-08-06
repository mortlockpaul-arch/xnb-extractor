using System;
using System.Collections.Generic;
using System.Text;

namespace XnbExtractor.Content;

public class XnbTexture
{
    public int RawFormat { get; set; }

    public XboxSurfaceFormat? XboxFormat { get; set; }

    public SurfaceFormat? WindowsFormat { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int MipCount { get; set; }

    public List<byte[]> MipData { get; } = new();
}