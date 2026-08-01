using System;
using System.IO;
using XnbExtractor.Readers;
using XnbExtractor.Xnb;

namespace XnbExtractor.Content;

public class XnbContentReader : IDisposable
{
    private readonly BinaryReader _reader;

    public XnbContentReader(Stream stream)
    {
        _reader = new BinaryReader(stream);
    }




    public byte ReadByte()
    {
        return _reader.ReadByte();
    }


    public int ReadInt32()
    {
        return _reader.ReadInt32();
    }

    public string ReadString()
    {
        return _reader.ReadString();
    }

    public int Read7BitEncodedInt()
    {
        // BinaryReader.Read7BitEncodedInt is protected; implement locally
        int count = 0;
        int shift = 0;
        while (shift < 35)
        {
            byte b = _reader.ReadByte();
            count |= (b & 0x7F) << shift;
            shift += 7;
            if ((b & 0x80) == 0)
                return count;
        }
        throw new FormatException("Bad 7-bit encoded integer in stream.");
    }

    public byte[] ReadBytes(int count)
    {
        return _reader.ReadBytes(count);
    }

    public long Position => _reader.BaseStream.Position;

    public void Seek(long position)
    {
        _reader.BaseStream.Seek(position, SeekOrigin.Begin);
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
    public static XnbContent Parse(byte[] data)
    {
        using var reader = new XnbContentReader(new MemoryStream(data));

        var content = new XnbContent();

        // XNB Header
        var magic = reader.ReadBytes(3);

        if (magic[0] != 'X' ||
            magic[1] != 'N' ||
            magic[2] != 'B')
        {
            throw new InvalidDataException("Not an XNB file");
        }

        char platform = (char)reader.ReadByte();
        byte version = reader.ReadByte();
        byte flags = reader.ReadByte();
        int fileSize = reader.ReadInt32();

        Console.WriteLine($"XNB {platform} v{version}");
        Console.WriteLine($"Flags: {flags:X2}");
        Console.WriteLine($"File size: {fileSize}");

        // Reader list
        int readerCount = reader.Read7BitEncodedInt();

        Console.WriteLine($"Reader count: {readerCount}");

        for (int i = 0; i < readerCount; i++)
        {
            string readerType = reader.ReadString();

            content.Readers.Add(readerType);

            int readerVersion = reader.ReadInt32();

            Console.WriteLine($"{i}: {readerType}");
        }

        content.SharedResourceCount = reader.Read7BitEncodedInt();

        content.PrimaryReaderIndex = reader.Read7BitEncodedInt();

        content.DataPosition = reader.Position;

        return content;
    }

}