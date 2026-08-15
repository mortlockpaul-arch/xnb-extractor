
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        Console.WriteLine(new string('=', 80));
        return value;
    }
    //public uint ReadUInt32() => reader.ReadUInt32();
    public int Read7BitEncodedInt()
    {
        int value = 0;
        int shift = 0;

        while (true)
        {
            byte b = ReadByte();
            value |= (b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                return value;

            shift += 7;

            if (shift >= 35)
                throw new FormatException("Invalid 7-bit encoded integer.");
        }
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

    public object ReadObject(int readerIndex)
    {
        if (readerIndex < 0 || readerIndex >= content.Readers.Count)
            throw new ArgumentOutOfRangeException(nameof(readerIndex));

        string type = content.Readers[readerIndex].Type;

        Console.WriteLine($"ReadObject(readerIndex={readerIndex}) -> {type}");

        return XnbTypeReaderFactory.ReadByType(type, this);
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



    public List<T> ReadList<T>(int elementReaderIndex)
    {
        int count = ReadInt32Platform();

        Console.WriteLine(
            $"Reading List<{typeof(T).Name}> count={count}, reader={elementReaderIndex}");

        var list = new List<T>(count);

        for (int i = 0; i < count; i++)
            list.Add((T)ReadObject(elementReaderIndex));

        return list;
    }

    public Rectangle ReadRectangle() =>
        new(
            ReadInt32(),
            ReadInt32(),
            ReadInt32(),
            ReadInt32());

    public Rectangle ReadRectanglePlatform() =>
        new(
            ReadInt32Platform(),
            ReadInt32Platform(),
            ReadInt32Platform(),
            ReadInt32Platform());

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

    public byte ReadByte() => reader.ReadByte();

    public sbyte ReadSByte() => reader.ReadSByte();

    public bool ReadBoolean() => reader.ReadBoolean();

    public ushort ReadUInt16() => reader.ReadUInt16();

    public short ReadInt16() => reader.ReadInt16();

    public uint ReadUInt32() => reader.ReadUInt32();

    public int ReadInt32() => reader.ReadInt32();

    public ulong ReadUInt64() => reader.ReadUInt64();

    public long ReadInt64() => reader.ReadInt64();

    public float ReadSingle() => reader.ReadSingle();

    public double ReadDouble() => reader.ReadDouble();

    public char ReadChar() => reader.ReadChar();

    public byte[] ReadBytes(int count) => reader.ReadBytes(count);

    private static ushort Swap(ushort value) =>
        (ushort)((value >> 8) | (value << 8));

    private static uint Swap(uint value) =>
        ((value >> 24) & 0x000000FF) |
        ((value >> 8) & 0x0000FF00) |
        ((value << 8) & 0x00FF0000) |
        ((value << 24) & 0xFF000000);

    private static ulong Swap(ulong value) =>
        ((value & 0x00000000000000FFUL) << 56) |
        ((value & 0x000000000000FF00UL) << 40) |
        ((value & 0x0000000000FF0000UL) << 24) |
        ((value & 0x00000000FF000000UL) << 8) |
        ((value & 0x000000FF00000000UL) >> 8) |
        ((value & 0x0000FF0000000000UL) >> 24) |
        ((value & 0x00FF000000000000UL) >> 40) |
        ((value & 0xFF00000000000000UL) >> 56);

    public ushort ReadUInt16Platform()
    {
        ushort value = reader.ReadUInt16();
        return IsBigEndian ? Swap(value) : value;
    }

    public short ReadInt16Platform() =>
        unchecked((short)ReadUInt16Platform());

    public uint ReadUInt32Platform()
    {
        uint value = reader.ReadUInt32();
        return IsBigEndian ? Swap(value) : value;
    }

    public int ReadInt32Platform() =>
        unchecked((int)ReadUInt32Platform());

    public ulong ReadUInt64Platform()
    {
        ulong value = reader.ReadUInt64();
        return IsBigEndian ? Swap(value) : value;
    }

    public long ReadInt64Platform() =>
        unchecked((long)ReadUInt64Platform());

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

    public Vector2 ReadVector2() =>
    new(ReadSingle(), ReadSingle());

    public Vector3 ReadVector3() =>
        new(ReadSingle(), ReadSingle(), ReadSingle());

    public Vector4 ReadVector4() =>
        new(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

    public Vector2 ReadVector2Platform() =>
        new(ReadSinglePlatform(), ReadSinglePlatform());

    public Vector3 ReadVector3Platform() =>
        new(ReadSinglePlatform(), ReadSinglePlatform(), ReadSinglePlatform());

    public Vector4 ReadVector4Platform() =>
        new(ReadSinglePlatform(), ReadSinglePlatform(), ReadSinglePlatform(), ReadSinglePlatform());
}