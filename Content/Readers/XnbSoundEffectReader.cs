using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XnbExtractor.Content;
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

            sound.FormatTag = reader.ReadUInt16Platform();
            sound.Channels = reader.ReadUInt16Platform();
            sound.SampleRate = reader.ReadUInt32Platform();
            sound.AverageBytesPerSecond = reader.ReadUInt32Platform();
            sound.BlockAlign = reader.ReadUInt16Platform();
            sound.BitsPerSample = reader.ReadUInt16Platform();

            if (formatLength > 16)
            {
                ushort cbSize = reader.ReadUInt16Platform();

                if (sound.FormatTag == 0x166 && cbSize == 34)
                {
                    Console.WriteLine("Converting XMA2 extra data...");

                    using var ms = new MemoryStream();
                    using var bw = new BinaryWriter(ms);

                    // XMA2WAVEFORMATEX
                    bw.Write(reader.ReadUInt16Platform());
                    bw.Write(reader.ReadUInt32Platform());
                    bw.Write(reader.ReadUInt32Platform());
                    bw.Write(reader.ReadUInt32Platform());
                    bw.Write(reader.ReadUInt32Platform());
                    bw.Write(reader.ReadUInt32Platform());
                    bw.Write(reader.ReadUInt32Platform());
                    bw.Write(reader.ReadUInt32Platform());

                    // Remaining byte fields
                    bw.Write(reader.ReadByte());
                    bw.Write(reader.ReadByte());

                    bw.Write(reader.ReadUInt16Platform());

                    sound.ExtraData = ms.ToArray();
                }
                else
                {
                    sound.ExtraData = reader.ReadBytes(cbSize);
                }

                int remaining = (int)formatLength - 18 - cbSize;

                if (remaining > 0)
                    reader.ReadBytes(remaining);
            }

            int dataLength = reader.ReadInt32();
            sound.AudioData = reader.ReadBytes(dataLength);

            sound.LoopStart = reader.ReadInt32();
            sound.LoopLength = reader.ReadInt32();
            sound.DurationMs = reader.ReadUInt32();

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
