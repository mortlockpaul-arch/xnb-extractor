using System;
using System.Collections.Generic;
using System.Text;

namespace XnbExtractor.Content;

public enum XboxSurfaceFormat
{
    Unknown = -1,

    Color = 1,
    Bgr32 = 2,
    Bgra1010102 = 3,
    Rgba32 = 4,
    Rgb32 = 5,
    Rgba1010102 = 6,
    Rg32 = 7,
    Rgba64 = 8,
    Bgr565 = 9,
    Bgra5551 = 10,
    Bgr555 = 11,
    Bgra4444 = 12,
    Bgr444 = 13,
    Alpha8 = 15,

    Dxt1 = 28,
    Dxt3 = 30,
    Dxt5 = 32,

    Single = 22,
    Vector2 = 23,
    Vector4 = 24,
    HalfSingle = 25,
    HalfVector2 = 26,
    HalfVector4 = 27
}
public enum SurfaceFormat
{
    Color = 0,
    Bgr565 = 1,
    Bgra5551 = 2,
    Bgra4444 = 3,
    Dxt1 = 4,
    Dxt3 = 5,
    Dxt5 = 6,
    NormalizedByte2 = 7,
    NormalizedByte4 = 8,
    Rgba1010102 = 9,
    Rg32 = 10,
    Rgba64 = 11,
    Alpha8 = 12,
    Single = 13,
    Vector2 = 14,
    Vector4 = 15,
    HalfSingle = 16,
    HalfVector2 = 17,
    HalfVector4 = 18,
    HdrBlendable = 19,
}

