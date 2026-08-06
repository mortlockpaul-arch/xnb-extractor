using System;
using XnbExtractor.Content;
using XnbExtractor.Models;

namespace XnbExtractor.Content.Readers;

public class XnbTEdgePalReader : IXnbTypeReader
{
    public object Read(XnbContentReader reader)
    {
        Console.WriteLine($"Reading EdgePal at {reader.Position}");
        uint[] colors = new uint[256];

        for (int i = 0; i < 256; i++)
        {
            colors[i] = reader.ReadUInt32();
        }
        Console.WriteLine($"First colour: 0x{colors[0]:X8}");
        Console.WriteLine($"Last colour: 0x{colors[255]:X8}");
        return new EdgePal(colors);
    }
}