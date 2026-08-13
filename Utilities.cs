using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XnbExtractor.Content;
using XnbExtractor.Xnb;

namespace XnbExtractor
{
    public class Utilities
    {
        public enum DetectedTextureFormat
        {
            Unknown,
            Color,
            Bgr565,
            Dxt1,
            Dxt3,
            Dxt5
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }

        public static string GetRelativePath(string basePath, string path)
        {
            var baseUri = new Uri(AppendDirectorySeparatorChar(basePath));
            var pathUri = new Uri(path);

            return Uri.UnescapeDataString(
                baseUri.MakeRelativeUri(pathUri)
                       .ToString()
                       .Replace('/', Path.DirectorySeparatorChar));
        }

        public static class TextureFormatDetector
        {
            public enum XboxSurfaceFormat
            {
                Unknown = -1,

                Color = 0,
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

                Single = 22,
                Vector2 = 23,
                Vector4 = 24,
                HalfSingle = 25,
                HalfVector2 = 26,
                HalfVector4 = 27,

                Dxt1 = 28,
                Dxt3 = 30,
                Dxt5 = 32
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

                Alpha8 = 7,
                Rgba32 = 8,
                Rgb32 = 9,
                Bgra1010102 = 10,
                Rgba1010102 = 11,
                Rg32 = 12,
                Rgba64 = 13,
                Single = 14,
                Vector2 = 15,
                Vector4 = 16,
                HalfSingle = 17,
                HalfVector2 = 18,
                HalfVector4 = 19
            }
            public enum TextureFormat
            {
                Unknown,

                Color,
                Bgr565,
                Bgra5551,
                Bgra4444,

                Dxt1,
                Dxt3,
                Dxt5,

                Alpha8,

                Rgba32,
                Rgb32,
                Bgr32,
                Bgra1010102,
                Rgba1010102,

                Rg32,
                Rgba64,

                Single,
                Vector2,
                Vector4,
                HalfSingle,
                HalfVector2,
                HalfVector4,
                Bgr555,
                Bgr444
            }

            public static SurfaceFormat ToWindowsFormat(TextureFormat format)
            {
                return format switch
                {
                    TextureFormat.Color => SurfaceFormat.Color,
                    TextureFormat.Bgr565 => SurfaceFormat.Bgr565,
                    TextureFormat.Bgra5551 => SurfaceFormat.Bgra5551,
                    TextureFormat.Bgra4444 => SurfaceFormat.Bgra4444,

                    TextureFormat.Dxt1 => SurfaceFormat.Dxt1,
                    TextureFormat.Dxt3 => SurfaceFormat.Dxt3,
                    TextureFormat.Dxt5 => SurfaceFormat.Dxt5,

                    TextureFormat.Alpha8 => SurfaceFormat.Alpha8,

                    TextureFormat.Rgba32 => SurfaceFormat.Rgba32,
                    TextureFormat.Rgb32 => SurfaceFormat.Rgb32,

                    TextureFormat.Bgra1010102 => SurfaceFormat.Bgra1010102,
                    TextureFormat.Rgba1010102 => SurfaceFormat.Rgba1010102,

                    TextureFormat.Rg32 => SurfaceFormat.Rg32,
                    TextureFormat.Rgba64 => SurfaceFormat.Rgba64,

                    TextureFormat.Single => SurfaceFormat.Single,
                    TextureFormat.Vector2 => SurfaceFormat.Vector2,
                    TextureFormat.Vector4 => SurfaceFormat.Vector4,
                    TextureFormat.HalfSingle => SurfaceFormat.HalfSingle,
                    TextureFormat.HalfVector2 => SurfaceFormat.HalfVector2,
                    TextureFormat.HalfVector4 => SurfaceFormat.HalfVector4,

                    _ => throw new NotSupportedException(
                        $"No Windows SurfaceFormat mapping for {format}")
                };
            }
            public static TextureFormat Detect(TargetPlatform platform, int rawFormat)
            {
                if (platform == TargetPlatform.Xbox360)
                {
                    return rawFormat switch
                    {
                        0 => TextureFormat.Color,

                        1 => TextureFormat.Color,
                        2 => TextureFormat.Bgr32,
                        3 => TextureFormat.Bgra1010102,
                        4 => TextureFormat.Rgba32,
                        5 => TextureFormat.Rgb32,
                        6 => TextureFormat.Rgba1010102,
                        7 => TextureFormat.Rg32,
                        8 => TextureFormat.Rgba64,
                        9 => TextureFormat.Bgr565,
                        10 => TextureFormat.Bgra5551,
                        11 => TextureFormat.Bgr555,
                        12 => TextureFormat.Bgra4444,
                        13 => TextureFormat.Bgr444,
                        15 => TextureFormat.Alpha8,

                        28 => TextureFormat.Dxt1,
                        30 => TextureFormat.Dxt3,
                        32 => TextureFormat.Dxt5,

                        22 => TextureFormat.Single,
                        23 => TextureFormat.Vector2,
                        24 => TextureFormat.Vector4,
                        25 => TextureFormat.HalfSingle,
                        26 => TextureFormat.HalfVector2,
                        27 => TextureFormat.HalfVector4,

                        _ => TextureFormat.Unknown
                    };
                }

                return rawFormat switch
                {
                    0 => TextureFormat.Color,
                    1 => TextureFormat.Bgr565,
                    2 => TextureFormat.Bgra5551,
                    3 => TextureFormat.Bgra4444,
                    4 => TextureFormat.Dxt1,
                    5 => TextureFormat.Dxt3,
                    6 => TextureFormat.Dxt5,
                    7 => TextureFormat.Alpha8,
                    8 => TextureFormat.Rgba32,
                    9 => TextureFormat.Rgb32,
                    10 => TextureFormat.Bgra1010102,
                    11 => TextureFormat.Rgba1010102,
                    12 => TextureFormat.Rg32,
                    13 => TextureFormat.Rgba64,
                    14 => TextureFormat.Single,
                    15 => TextureFormat.Vector2,
                    16 => TextureFormat.Vector4,
                    17 => TextureFormat.HalfSingle,
                    18 => TextureFormat.HalfVector2,
                    19 => TextureFormat.HalfVector4,

                    _ => TextureFormat.Unknown
                };
            }
            class ExtractOptions
            {
                public bool Loader { get; set; }
                public bool Parser { get; set; }
                public bool WriteDds { get; set; }
                public bool Overwrite { get; set; }
            }
        }
    }
}
