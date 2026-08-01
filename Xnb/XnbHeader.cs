using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XnbExtractor.Xnb;

    public class XnbHeader
    {
        public char Platform { get; set; }
        public byte Version { get; set; }
        public byte Flags { get; set; }

        public bool IsCompressed => (Flags & 0x80) != 0;

        public int PayloadOffset { get; set; }

        public int CompressedSize { get; set; }

        public int DecompressedSize { get; set; }

        public int WindowSize { get; set; } = 16;
        public static XnbHeader Parse(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            if (new string(reader.ReadChars(3)) != "XNB")
                throw new InvalidDataException("Not an XNB file.");

            var header = new XnbHeader
            {
                Platform = (char)reader.ReadByte(),
                Version = reader.ReadByte(),
                Flags = reader.ReadByte(),
            };

            int fileSize = reader.ReadInt32();
            header.CompressedSize = fileSize;

            if (header.IsCompressed)
            {
                header.DecompressedSize = reader.ReadInt32();
            }
            else
            {
                header.DecompressedSize = fileSize;
            }

            header.PayloadOffset = (int)reader.BaseStream.Position;

            return header;
        }

        public override string ToString()
        {
            return
                $"Platform: {Platform}\n" +
                $"Version: {Version}\n" +
                $"Flags: 0x{Flags:X2}\n" +
                $"Compressed: {IsCompressed}\n" +
                $"Compressed Size: {CompressedSize}\n" +
                $"Decompressed Size: {DecompressedSize}\n" +
                $"Payload Offset: {PayloadOffset}";
        }
    }
