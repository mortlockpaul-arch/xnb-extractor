using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Contexts;
using System.Text;
using XnbExtractor.Content.Readers;
using XnbExtractor.Readers;
using XnbExtractor.Xnb;

namespace XnbExtractor.Content;


public interface IXnbTypeReader
{
    object Read(XnbContentReader reader);
}

public class XnbContentReader : IDisposable
{

    private readonly BinaryReader reader;

    private readonly XnbContent content;
    public long Length => reader.BaseStream.Length;
    public long Position => reader.BaseStream.Position;

    public long Remaining
    {
        get
        {
            return Length - Position;
        }
    }


    public TargetPlatform Platform { get; internal set; }

    public XnbContentReader( Stream stream, XnbContent content, TargetPlatform platform)
    {
        reader = new BinaryReader(stream);
        this.content = content;
        Platform = platform;
    }

    public byte ReadByte()
    {
        return reader.ReadByte();
    }

    public int ReadInt32()
    {
        return reader.ReadInt32();
    }


    public string ReadString()
    {
        int length = Read7BitEncodedInt();

        Console.WriteLine($"String length: {length}");

        if (length == 0)
            return string.Empty;

        byte[] bytes = reader.ReadBytes(length);

        string value = Encoding.UTF8.GetString(bytes);

        Console.WriteLine($"String: '{value}'");

        return value;
    }
    public uint ReadUInt32() => reader.ReadUInt32();
    public int Read7BitEncodedInt()
    {
        // BinaryReader.Read7BitEncodedInt is protected; implement locally
        int count = 0;
        int shift = 0;
        while (shift < 35)
        {
            byte b = reader.ReadByte();
            count |= (b & 0x7F) << shift;
            shift += 7;
            if ((b & 0x80) == 0)
                return count;
        }
        throw new FormatException("Bad 7-bit encoded integer in stream.");
    }

    public byte[] ReadBytes(int count)
    {
        return reader.ReadBytes(count);
    }

    public void Seek(long position)
    {
        reader.BaseStream.Seek(position, SeekOrigin.Begin);
    }
    public T? ReadObject<T>()
    {
        var obj = ReadObject();

        if (obj == null)
            return default;

        return (T)obj;
    }

    public object? ReadObject()
    {
        int rawIndex = Read7BitEncodedInt();

        Console.WriteLine($"Object reader index raw: {rawIndex}");

        if (rawIndex == 0)
        {
            Console.WriteLine("Object = null");
            return null;
        }

        int readerIndex = rawIndex - 1;

        var type = content.Readers[readerIndex];

        Console.WriteLine(
            $"ReadObject -> Reader {readerIndex}: {type}");

        return XnbTypeReaderFactory.ReadByType(type, this);
    }


    public void Dispose()
    {
        reader.Dispose();
    }

    //public static T LoadXnb<T>(string filename)
    //{
    //    // Parse header
    //    // Decompress if needed
    //    // Parse reader table
    //    // Return the object produced by the primary reader

    //    return (T)XnbTypeReaderFactory.ReadByType(
    //        content.Readers[content.PrimaryReaderIndex - 1],
    //        reader);
    //}

    public static XnbContent ParseXnbContent(byte[] xnbData)
    {
        // Read header
        // Decompress if needed
        // Call ParseContent(...)
        var content = ParseContent(xnbData);
        return content;
    }

    public static XnbContent ParseContent(byte[] contentData)
    {
        var content = new XnbContent();

        using var reader = new XnbContentReader(
            new MemoryStream(contentData),
            content,
            TargetPlatform.Windows);
        int readerCount = reader.Read7BitEncodedInt();
        Console.WriteLine($"Reader.Platform = {reader.Platform}");
        Console.WriteLine($"Xbox enum value = {(int)TargetPlatform.Xbox360}");
        Console.WriteLine($"Reader value    = {(int)reader.Platform}");
        Console.WriteLine($"Reader count: {readerCount}");

        for (int i = 0; i < readerCount; i++)
        {
            string readerType = reader.ReadString();
            content.Readers.Add(readerType);

            int readerVersion = reader.ReadInt32();

            Console.WriteLine($"{i}: {readerType}");
        }

        content.SharedResourceCount = reader.Read7BitEncodedInt();
        content.PrimaryReaderIndex = reader.ReadByte();
        content.DataPosition = reader.Position;

        Console.WriteLine(
    $"Shared={content.SharedResourceCount}, Primary={content.PrimaryReaderIndex}");
        Console.WriteLine($"Data starts at: {content.DataPosition}");

        return content;
    }

    public bool ReadBoolean()
    {
        throw new NotImplementedException();
    }

    public float ReadSingle()
    {
        throw new NotImplementedException();
    }
}