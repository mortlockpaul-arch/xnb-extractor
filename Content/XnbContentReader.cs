
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.Remoting.Contexts;
using System.Text;
using XnbExtractor.Content.Readers;
using XnbExtractor.Content.Writers;
using XnbExtractor.Readers;


namespace XnbExtractor.Content;


public interface IXnbTypeReader
{
    object Read(XnbContentReader reader);
}

public enum TargetPlatform
{
    Zune = 3,
    Xbox360 = 2,
    Windows = 1,
    Unknown = 0
}

public class XnbContentReader : IDisposable
{
    public Stream BaseStream => reader.BaseStream;
    private readonly BinaryReader reader;
    public XnbContent Content => content;
    private readonly XnbContent content;
    public long Length => reader.BaseStream.Length;
    public long Position => reader.BaseStream.Position;
    public bool IsBigEndian => Platform == TargetPlatform.Xbox360;
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
    public ushort ReadUInt16()
    {
        return reader.ReadUInt16();
    }
    public int ReadInt32()
    {
        return reader.ReadInt32();
    }

    public Vector2 ReadVector2()
    {
        Vector2 result = new Vector2();
        result.X = ReadSingle();
        result.Y = ReadSingle();
        return result;
    }

    public Vector3 ReadVector3()
    {
        Vector3 result = new Vector3();
        result.X = ReadSingle();
        result.Y = ReadSingle();
        result.Z = ReadSingle();
        return result;
    }

    public Vector4 ReadVector4()
    {
        Vector4 result = new Vector4();
        result.X = ReadSingle();
        result.Y = ReadSingle();
        result.Z = ReadSingle();
        result.W = ReadSingle();
        return result;
    }

    //public Color ReadColor()
    //{
    //    Color result = new Color();
    //    result.R = ReadByte();
    //    result.G = ReadByte();
    //    result.B = ReadByte();
    //    result.A = ReadByte();
    //    return result;
    //}

    public static class Log
    {
        public static bool Verbose = true;

        public static void Write(string text)
        {
            if (Verbose)
                Console.WriteLine(text);
        }
    }
    public string ReadString()
    {
        Console.WriteLine("*** XnbContentReader.ReadString ***");
        Console.WriteLine($"ReadString starts at {Position}");
        int length = Read7BitEncodedInt();

        Log.Write($"String length: {length}");

        if (length == 0)
            return string.Empty;

        byte[] bytes = reader.ReadBytes(length);

        string value = Encoding.UTF8.GetString(bytes);

        if (value.Length < 200)
        {
            Log.Write($"String: '{value}'");
        }
        else
        {
            Log.Write($"String ({value.Length} chars)");
        }
        return value;
    }
    public uint ReadUInt32() => reader.ReadUInt32();
    public int Read7BitEncodedInt()
    {
        int count = 0;
        int shift = 0;

        Console.Write($"7-bit bytes @ {Position}: ");

        while (shift < 35)
        {
            byte b = reader.ReadByte();

            Console.Write($"{b:X2} ");

            count |= (b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                Console.WriteLine($" -> {count}");
                return count;
            }

            shift += 7;
        }

        throw new FormatException();
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
        Console.WriteLine(content.Readers.Count);
        Console.WriteLine($"Object reader index raw: {rawIndex}");

        if (rawIndex == 0)
        {
            Console.WriteLine("Object = null");
            return null;
        }

        int readerIndex = rawIndex - 1;

        var type = content.Readers[readerIndex].Type;

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

    //public static XnbContent ParseXnbContent(byte[] xnbData)
    //{
    //    // Read header
    //    // Decompress if needed
    //    // Call ParseContent(...)
    //    var content = ParseContent(xnbData);
    //    return content;
    //}

    public static void ParseContent(XnbContentReader reader)
    {
        var content = reader.Content;

        content.Readers.Clear();

        int readerCount = reader.Read7BitEncodedInt();
        Log.Write($"Reader.Platform = {reader.Platform}");
        Log.Write($"Xbox enum value = {(int)TargetPlatform.Xbox360}");
        Log.Write($"Reader value    = {(int)reader.Platform}");
        Console.WriteLine($"ReaderCount = {readerCount}");
        Console.WriteLine($"Position after count = {reader.Position}");
        Console.WriteLine($"Length   = {reader.Length}");

        for (int i = 0; i < readerCount; i++)
        {
            string readerType = reader.ReadString();
            int readerVersion = reader.ReadInt32();

            content.Readers.Add(new XnbReaderInfo
            {
                Type = readerType,
                Version = readerVersion
            });

            Log.Write(
                $"{i}: {readerType}, Version={readerVersion}");
        }

        content.SharedResourceCount = reader.Read7BitEncodedInt();
        content.PrimaryReaderIndex = reader.ReadByte();
        content.DataPosition = reader.Position;

        Log.Write(
    $"Shared={content.SharedResourceCount}, Primary={content.PrimaryReaderIndex}");
        Log.Write($"Data starts at: {content.DataPosition}");

    }

    private static ushort Swap(ushort value)
    {
        return (ushort)(
            ((value >> 8) & 0x00FF) |
            ((value << 8) & 0xFF00)
        );
    }

    private static uint Swap(uint value)
    {
        return
            ((value >> 24) & 0x000000FF) |
            ((value >> 8) & 0x0000FF00) |
            ((value << 8) & 0x00FF0000) |
            ((value << 24) & 0xFF000000);
    }

    private static ulong Swap(ulong value)
    {
        return
            ((value & 0x00000000000000FFUL) << 56) |
            ((value & 0x000000000000FF00UL) << 40) |
            ((value & 0x0000000000FF0000UL) << 24) |
            ((value & 0x00000000FF000000UL) << 8) |
            ((value & 0x000000FF00000000UL) >> 8) |
            ((value & 0x0000FF0000000000UL) >> 24) |
            ((value & 0x00FF000000000000UL) >> 40) |
            ((value & 0xFF00000000000000UL) >> 56);
    }
    public ushort ReadUInt16Platform()
    {
        ushort value = reader.ReadUInt16();
        return IsBigEndian ? Swap(value) : value;
    }

    public uint ReadUInt32Platform()
    {
        uint value = reader.ReadUInt32();
        return IsBigEndian ? Swap(value) : value;
    }

    public short ReadInt16Platform()
    {
        return unchecked((short)ReadUInt16Platform());
    }

    public int ReadInt32Platform()
    {
        return unchecked((int)ReadUInt32Platform());
    }

    public ulong ReadUInt64Platform()
    {
        ulong value = reader.ReadUInt64();
        return IsBigEndian ? Swap(value) : value;
    }

    public long ReadInt64Platform()
    {
        return unchecked((long)ReadUInt64Platform());
    }

    public float ReadSinglePlatform()
    {
        byte[] bytes = reader.ReadBytes(4);

        if (IsBigEndian)
            Array.Reverse(bytes);

        return BitConverter.ToSingle(bytes, 0);
    }

    public double ReadDoublePlatform()
    {
        byte[] bytes = reader.ReadBytes(8);

        if (IsBigEndian)
            Array.Reverse(bytes);

        return BitConverter.ToDouble(bytes, 0);
    }
    public float ReadSingle()
    {
        return reader.ReadSingle();
    }

    public bool ReadBoolean()
    {
        return reader.ReadBoolean();
    }

    public char ReadChar()
    {
        return reader.ReadChar();
    }

    //internal T ReadObject<T>(ContentTypeReader elementReader)
    //{
    //    throw new NotImplementedException();
    //}
}