using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace XnbExtractor.Writers
{
    public static class PngWriter
    {
        private static readonly byte[] PngSignature =
        {
            137, 80, 78, 71,
            13, 10, 26, 10
        };

        public static void Write(
            string filename,
            int width,
            int height,
            byte[] rgba)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));

            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            if (rgba == null)
                throw new ArgumentNullException(nameof(rgba));

            long expectedLength = (long)width * height * 4;

            if (rgba.Length != expectedLength)
            {
                throw new ArgumentException(
                    $"Invalid RGBA data length. " +
                    $"Expected {expectedLength} bytes, got {rgba.Length}.");
            }

            string? directory = Path.GetDirectoryName(filename);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using (var output = File.Create(filename))
            using (var writer = new BinaryWriter(output))
            {
                // ---------------------------------------------------------
                // PNG signature
                // ---------------------------------------------------------

                writer.Write(PngSignature);

                // ---------------------------------------------------------
                // IHDR
                // ---------------------------------------------------------

                using (var ihdr = new MemoryStream())
                using (var ihdrWriter = new BinaryWriter(ihdr))
                {
                    WriteUInt32(ihdrWriter, (uint)width);
                    WriteUInt32(ihdrWriter, (uint)height);

                    // Bit depth
                    ihdrWriter.Write((byte)8);

                    // Color type 6 = RGBA
                    ihdrWriter.Write((byte)6);

                    // Compression method
                    ihdrWriter.Write((byte)0);

                    // Filter method
                    ihdrWriter.Write((byte)0);

                    // Interlace method
                    ihdrWriter.Write((byte)0);

                    WriteChunk(
                        writer,
                        "IHDR",
                        ihdr.ToArray());
                }

                // ---------------------------------------------------------
                // Build raw scanline data
                //
                // Every PNG scanline starts with a filter byte.
                // 0 = None.
                // ---------------------------------------------------------

                byte[] raw = new byte[
                    (long)height * (width * 4 + 1)];

                int sourceOffset = 0;
                int destinationOffset = 0;

                for (int y = 0; y < height; y++)
                {
                    // PNG filter type: None
                    raw[destinationOffset++] = 0;

                    Buffer.BlockCopy(
                        rgba,
                        sourceOffset,
                        raw,
                        destinationOffset,
                        width * 4);

                    sourceOffset += width * 4;
                    destinationOffset += width * 4;
                }

                // ---------------------------------------------------------
                // Compress raw scanline data using DEFLATE.
                //
                // PNG IDAT contains a ZLIB stream, not just raw DEFLATE.
                //
                // .NET Framework 4.8.1 doesn't have ZLibStream, so we
                // manually write:
                //
                //   zlib header
                //   DEFLATE data
                //   Adler-32
                // ---------------------------------------------------------

                byte[] compressed;

                using (var compressedStream = new MemoryStream())
                {
                    // ZLIB header.
                    //
                    // CMF = 0x78
                    // FLG = 0x9C
                    //
                    // This represents the normal DEFLATE/32K configuration.
                    compressedStream.WriteByte(0x78);
                    compressedStream.WriteByte(0x9C);

                    using (var deflate = new DeflateStream(
                        compressedStream,
                        CompressionLevel.Optimal,
                        true))
                    {
                        deflate.Write(
                            raw,
                            0,
                            raw.Length);
                    }

                    // Adler-32 is calculated over the UNCOMPRESSED
                    // scanline data.
                    uint adler = Adler32(raw);

                    WriteUInt32(
                        compressedStream,
                        adler);

                    compressed = compressedStream.ToArray();
                }

                // ---------------------------------------------------------
                // IDAT
                // ---------------------------------------------------------

                WriteChunk(
                    writer,
                    "IDAT",
                    compressed);

                // ---------------------------------------------------------
                // IEND
                // ---------------------------------------------------------

                WriteChunk(
                    writer,
                    "IEND",
                    Array.Empty<byte>());
            }
        }

        // ================================================================
        // PNG Chunk
        // ================================================================

        private static void WriteChunk(
            BinaryWriter writer,
            string type,
            byte[] data)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type.Length != 4)
                throw new ArgumentException(
                    "PNG chunk type must contain exactly 4 characters.",
                    nameof(type));

            byte[] typeBytes = Encoding.ASCII.GetBytes(type);

            // Length
            WriteUInt32(
                writer,
                (uint)data.Length);

            // Chunk type
            writer.Write(typeBytes);

            // Chunk data
            if (data.Length > 0)
                writer.Write(data);

            // CRC covers:
            //
            //   chunk type
            //   chunk data
            //
            uint crc = 0xFFFFFFFF;

            foreach (byte b in typeBytes)
                crc = UpdateCrc(crc, b);

            foreach (byte b in data)
                crc = UpdateCrc(crc, b);

            crc = ~crc;

            WriteUInt32(writer, crc);
        }

        // ================================================================
        // PNG uses big-endian integers.
        // ================================================================

        private static void WriteUInt32(
            BinaryWriter writer,
            uint value)
        {
            writer.Write((byte)((value >> 24) & 0xFF));
            writer.Write((byte)((value >> 16) & 0xFF));
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }

        // Overload for MemoryStream.
        private static void WriteUInt32(
            Stream stream,
            uint value)
        {
            stream.WriteByte((byte)((value >> 24) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)(value & 0xFF));
        }

        // ================================================================
        // Adler-32
        // ================================================================

        private static uint Adler32(byte[] data)
        {
            const uint Mod = 65521;

            uint a = 1;
            uint b = 0;

            foreach (byte value in data)
            {
                a += value;

                if (a >= Mod)
                    a -= Mod;

                b += a;

                if (b >= Mod)
                    b -= Mod;
            }

            return (b << 16) | a;
        }

        // ================================================================
        // CRC-32
        // ================================================================

        private static uint UpdateCrc(
            uint crc,
            byte value)
        {
            crc ^= value;

            for (int i = 0; i < 8; i++)
            {
                if ((crc & 1) != 0)
                {
                    crc =
                        (crc >> 1) ^
                        0xEDB88320;
                }
                else
                {
                    crc >>= 1;
                }
            }

            return crc;
        }
    }
}