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
