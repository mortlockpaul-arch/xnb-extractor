using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XnbExtractor;
using XnbExtractor.Content;
using XnbExtractor.Content.Readers;
using XnbExtractor.Models;
using XnbExtractor.Xnb;

namespace Monogame_Compression.Content.Readers
{
    public class XnbSoundEffectReader : IXnbTypeReader
    {
        public object Read(XnbContentReader reader)
        {
            var sound = new XnbSoundEffect();

            uint formatLength = reader.ReadUInt32();

            Console.WriteLine($"Format length: {formatLength}");

            sound.FormatTag = reader.ReadUInt16Platform();
            sound.Channels = reader.ReadUInt16Platform();
            sound.SampleRate = reader.ReadUInt32Platform();
            sound.AverageBytesPerSecond = reader.ReadUInt32Platform();
            sound.BlockAlign = reader.ReadUInt16Platform();
            sound.BitsPerSample = reader.ReadUInt16Platform();

            if (formatLength > 16)
            {
                ushort cbSize = reader.ReadUInt16Platform();
                Console.WriteLine($"cbSize: {cbSize}, FormatTag: 0x{sound.FormatTag:X4}");

                if (sound.FormatTag == 0x166 && cbSize == 34)
                {
                    using var ms = new MemoryStream();
                    using var bw = new BinaryWriter(ms);

                    bw.Write(reader.ReadUInt16Platform());
                    bw.Write(reader.ReadUInt32Platform());
                    bw.Write(reader.ReadUInt32Platform());
                    bw.Write(reader.ReadUInt32Platform());
                    bw.Write(reader.ReadUInt32Platform());
                    bw.Write(reader.ReadUInt32Platform());
                    bw.Write(reader.ReadUInt32Platform());
                    bw.Write(reader.ReadUInt32Platform());
                    bw.Write(reader.ReadByte());
                    bw.Write(reader.ReadByte());
                    bw.Write(reader.ReadUInt16Platform());

                    sound.ExtraData = ms.ToArray();
                }
                else
                {
                    sound.ExtraData = reader.ReadBytes(cbSize);
                }

                int remaining = checked((int)formatLength - 18 - cbSize);

                if (remaining < 0)
                    throw new InvalidDataException($"Invalid format length {formatLength}, cbSize {cbSize}");

                if (remaining > 0)
                    reader.ReadBytes(remaining);
            }

            byte[] lengthBytes = reader.ReadBytes(4);

            if (lengthBytes.Length != 4)
                throw new EndOfStreamException("Unable to read audio length.");

            int little = BinaryPrimitives.ReadInt32LittleEndian(lengthBytes);
            int big = BinaryPrimitives.ReadInt32BigEndian(lengthBytes);

            Console.WriteLine($"Length bytes: {Convert.ToHexString(lengthBytes)}");
            Console.WriteLine($"Little-endian: {little}");
            Console.WriteLine($"Big-endian: {big}");
            Console.WriteLine($"Remaining: {reader.BaseStream.Length - reader.BaseStream.Position}");

            int dataLength = little; // Temporarily test the little-endian interpretation.

            if (dataLength < 0 || dataLength > reader.BaseStream.Length - reader.BaseStream.Position - 12)
                throw new InvalidDataException($"Invalid audio length {dataLength}");

            sound.AudioData = reader.ReadBytes(dataLength);

            if (sound.AudioData.Length != dataLength)
                throw new EndOfStreamException($"Expected {dataLength} audio bytes, got {sound.AudioData.Length}");

            sound.LoopStart = reader.ReadInt32Platform();
            sound.LoopLength = reader.ReadInt32Platform();
            sound.DurationMs = reader.ReadUInt32Platform();

            Console.WriteLine($"FormatTag : 0x{sound.FormatTag:X4}");
            Console.WriteLine($"Channels  : {sound.Channels}");
            Console.WriteLine($"Rate      : {sound.SampleRate}");
            Console.WriteLine($"Bits      : {sound.BitsPerSample}");
            Console.WriteLine($"Data      : {sound.AudioData.Length} bytes");
            Console.WriteLine(
    BitConverter.ToString(sound.ExtraData));
            return sound;
        }

        static ushort Swap(bool swap, ushort x)
        {
            if (!swap) return x;

            return (ushort)(
                ((x >> 8) & 0x00FF) |
                ((x << 8) & 0xFF00));
        }

        static uint Swap(bool swap, uint x)
        {
            if (!swap) return x;

            return
                ((x >> 24) & 0x000000FF) |
                ((x >> 8) & 0x0000FF00) |
                ((x << 8) & 0x00FF0000) |
                ((x << 24) & 0xFF000000);
        }
    }
}
