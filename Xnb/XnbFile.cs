using System;
using System.Collections.Generic;
using System.Text;
using XnbExtractor.Content;
using Microsoft.Xna.Framework;
using System.IO;
using XnbExtractor.Compression;

namespace XnbExtractor.Xnb;

public class XnbFile : IDisposable
{

    private FileStream? stream;

    public void Dispose()
    {
        stream?.Dispose();
        stream = null;
    }

    public XnbHeader Header { get; }

    private byte[] Data { get; }

    public XnbFile(string file)
    {
        Data = File.ReadAllBytes(file);
        Header = XnbHeader.Parse(Data);
        for (int i = Header.PayloadOffset; i < Header.PayloadOffset + 32; i++)
        {
            Console.Write($"{Data[i]:X2} ");
        }
        Console.WriteLine();
        Console.WriteLine($"Data.Length = {Data.Length}");
        Console.WriteLine($"Header.CompressedSize = {Header.CompressedSize}");
    }

    public byte[] Decompress()
    {
        if (!Header.IsCompressed)
            return Data;

        using var input = new MemoryStream(Data);
        input.Position = Header.PayloadOffset;

        int compressedPayloadSize = Data.Length - Header.PayloadOffset;

        using var decoder = new LzxDecoderStream(
            input,
            Header.DecompressedSize,
            compressedPayloadSize);

        using var output = new MemoryStream();
        decoder.CopyTo(output);

        return output.ToArray();
    }

}
