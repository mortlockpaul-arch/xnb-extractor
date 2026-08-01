using System;
using System.IO;

namespace XnbExtractor.Xnb;

public class XnbContentReader : IDisposable
{
    private readonly BinaryReader reader;

    public long Position => reader.BaseStream.Position;

    public XnbContentReader(Stream stream)
    {
        reader = new BinaryReader(stream);
    }

    public XnbContentReader(byte[] data)
    {
        reader = new BinaryReader(new MemoryStream(data));
    }

    public int Remaining =>
    (int)(reader.BaseStream.Length - reader.BaseStream.Position);

    public void Seek(long position)
    {
        reader.BaseStream.Position = position;
    }

    public byte[] PeekBytes(int count)
    {
        long pos = reader.BaseStream.Position;

        byte[] data = reader.ReadBytes(count);

        reader.BaseStream.Position = pos;

        return data;
    }

    public byte[] ReadBytes(int count)
    {
        return reader.ReadBytes(count);
    }

    public int Read7BitEncodedInt()
    {
        int count = 0;
        int shift = 0;

        while (true)
        {
            byte b = reader.ReadByte();

            count |= (b & 0x7F) << shift;

            if ((b & 0x80) == 0)
                break;

            shift += 7;
        }

        return count;
    }

    public string ReadString() => reader.ReadString();

    public int ReadInt32() => reader.ReadInt32();

    public void Dispose()
    {
        reader.Dispose();
    }
}