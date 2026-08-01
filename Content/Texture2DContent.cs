using System;
using System.Collections.Generic;
using System.Text;
using XnbExtractor.Xnb;

namespace XnbExtractor.Content;

public class Texture2DContent
{
    public int SurfaceFormat { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int MipCount { get; set; }
    public List<byte[]> MipLevels { get; } = [];
}