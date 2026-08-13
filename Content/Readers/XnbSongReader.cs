using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XnbExtractor.Models;

namespace XnbExtractor.Content.Readers
{
    public class XnbSongReader : IXnbTypeReader
    {
        public object Read(XnbContentReader reader)
        {
            var song = new XnbSong();

            song.FileName = reader.ReadString();

            Console.WriteLine($"Song filename: {song.FileName}");

            song.Duration = reader.ReadInt32Platform();

            Console.WriteLine($"Song duration: {song.Duration} ms");
            Console.WriteLine($"Remaining: {reader.Remaining}");

            return song;
        }
    }
}
