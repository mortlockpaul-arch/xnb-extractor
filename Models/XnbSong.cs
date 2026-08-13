using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnbExtractor.Models
{
    public class XnbSong
    {
        public string AssetName { get; set; } = "";
        public int DurationMs { get; set; }
        public string FileName { get; internal set; }
        public int Duration { get; internal set; }

        public override string ToString()
        {
            return $"{AssetName} ({DurationMs} ms)";
        }
    }
}
