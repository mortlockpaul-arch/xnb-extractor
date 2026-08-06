using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnbExtractor
{
    internal class Utilities
    {
        class ExtractOptions
        {
            public bool Loader { get; set; }
            public bool Parser { get; set; }
            public bool WriteDds { get; set; }
            public bool Overwrite { get; set; }
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

        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }
    }
}
