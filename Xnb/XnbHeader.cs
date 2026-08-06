using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XnbExtractor.Xnb;

public enum TargetPlatform
{
    Windows,
    Xbox360
}
public class XnbHeader
    {
        public TargetPlatform Platform { get; set; }
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

            char platform = (char)reader.ReadByte();

            var header = new XnbHeader
            {
                Version = reader.ReadByte(),
                Flags = reader.ReadByte(),
                Platform = platform switch
                {
                    'w' => TargetPlatform.Windows,
                    'x' => TargetPlatform.Xbox360,
                    _ => throw new InvalidDataException(
                        $"Unknown XNB platform '{platform}' (0x{(int)platform:X2})")
                }
            };
        Console.WriteLine($"Version = {header.Version}");
        Console.WriteLine($"Platform character = '{platform}' (0x{(int)platform:X2})");
        Console.WriteLine($"Platform enum      = {header.Platform}");
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
