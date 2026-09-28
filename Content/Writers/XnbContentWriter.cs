using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XnbExtractor.Models;
using static XnbExtractor.Program.XnbCore;



namespace XnbExtractor.Content.Writers
{
    public sealed class XnbContentWriter : IDisposable
    {
        private readonly BinaryWriter writer;

        public XnbPlatform Platform { get; }

        public XnbContentWriter(Stream stream, XnbPlatform platform)
        {
            writer = new BinaryWriter(stream);
            Platform = platform;
        }

        public void Write7BitEncodedInt(int value)
        {
            uint v = (uint)value;

            while (v >= 0x80)
            {
                writer.Write((byte)(v | 0x80));
                v >>= 7;
            }

            writer.Write((byte)v);
        }

        public void WriteString(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);

            Write7BitEncodedInt(bytes.Length);
            writer.Write(bytes);
        }

        public void WriteInt32(int value)
        {
            writer.Write(value);
        }

        public void WriteBytes(byte[] data)
        {
            writer.Write(data);
        }

        public void WriteByte(byte value)
        {
            writer.Write(value);
        }
        public void WriteByte(int value)
        {
            writer.Write((byte)value);
        }
        public void Dispose()
        {
            ((IDisposable)writer).Dispose();
        }

        public long Position => writer.BaseStream.Position;

        public void Flush()
        {
            writer.Flush();
        }

        internal void WriteUInt32(uint value)
        {
            if (Platform == XnbPlatform.Xbox360)
            {
                writer.Write((byte)(value >> 24));
                writer.Write((byte)(value >> 16));
                writer.Write((byte)(value >> 8));
                writer.Write((byte)value);
            }
            else
            {
                writer.Write(value);
            }
        }
        internal void WriteUInt16(ushort value)
        {
            if (Platform == XnbPlatform.Xbox360)
            {
                writer.Write((byte)(value >> 8));
                writer.Write((byte)value);
            }
            else
            {
                writer.Write(value);
            }
        }

        public enum XnbPlatform
        {
            Windows = 0,
            Xbox360 = 2
        }
    }

    public sealed class XnbReaderInfo
    {
        public string Type { get; set; } = "";
        public int Version { get; set; }
    }

    public sealed class XnbWriter
    {
        private const byte XnbVersion = 5;

        private static void WriteEffect(
            XnbContentWriter writer,
            XnbEffect effect)
        {
            writer.WriteInt32(effect.Data.Length);
            writer.WriteBytes(effect.Data);
        }

        static void TestLoaderGame(LoadedAsset loaded, string inputFile)
        {
            try
            {
                if (Path.GetExtension(inputFile).Equals(".xnb", StringComparison.OrdinalIgnoreCase))
                {
                    var loaderGame = new LoaderGame(inputFile, loaded);
                    Console.WriteLine("LoaderGame loaded successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoaderGame failed: {ex}");
            }
        }

        internal static void Write( string outputFile, XnbContent content, LoadedAsset asset, bool runLoaderTest)
        {

            Console.WriteLine($"Exported Windows XNB: {outputFile}");
            if (runLoaderTest)
                TestLoaderGame(asset, outputFile);

            using var bodyStream = new MemoryStream();
            using var bodyWriter = new XnbContentWriter(bodyStream, XnbContentWriter.XnbPlatform.Windows);

            // ------------------------------------------------------------
            // Reader table
            // ------------------------------------------------------------

            bodyWriter.Write7BitEncodedInt(1);

            string readerType;

            switch (asset.Asset)
            {
                case XnbEffect:
                    readerType =
                        "Microsoft.Xna.Framework.Content.EffectReader, " +
                        "Microsoft.Xna.Framework.Graphics, " +
                        "Version=4.0.0.0, Culture=neutral, " +
                        "PublicKeyToken=842cf8be1de50553";
                    break;
                case XnbTexture texture:
                    readerType =
                        "Microsoft.Xna.Framework.Content.Texture2DReader, " +
                        "Microsoft.Xna.Framework.Graphics, " +
                        "Version=4.0.0.0, Culture=neutral, " +
                        "PublicKeyToken=842cf8be1de50553";
                    XnbTexture2DWriter.Write(bodyWriter, texture);
                    break;
                case XnbSoundEffect soundEffect:
                    readerType =
                        "Microsoft.Xna.Framework.Content.SoundEffectReader  , " +
                        "Microsoft.Xna.Framework.Graphics, " +
                        "Version=4.0.0.0, Culture=neutral, " +
                        "PublicKeyToken=842cf8be1de50553";
                    XnbSoundEffectWriter.Write(bodyWriter, soundEffect);
                    break;
                default:
                    throw new NotSupportedException(
                        $"Unsupported XNB asset type: {asset.GetType().FullName}");
            }

            bodyWriter.WriteString(readerType);

            // ContentTypeReader version
            bodyWriter.WriteInt32(0);

            // Shared resources
            bodyWriter.Write7BitEncodedInt(0);

            // Primary reader index
            // XNB reader indices are 1-based.
            bodyWriter.WriteByte(1);

            // ------------------------------------------------------------
            // Asset
            // ------------------------------------------------------------

            switch (asset.Asset)
            {
                case XnbEffect effect:
                    WriteEffect(bodyWriter, effect);
                    break;
            }

            bodyWriter.Flush();

            byte[] body = bodyStream.ToArray();

            // ------------------------------------------------------------
            // XNB header
            // ------------------------------------------------------------

            using var fs = File.Create(outputFile);
            using var writer = new BinaryWriter(fs);

            writer.Write((byte)'X');
            writer.Write((byte)'N');
            writer.Write((byte)'B');

            // Windows
            writer.Write((byte)'w');

            // XNA 4
            writer.Write((byte)5);

            // Flags: uncompressed
            writer.Write((byte)0);

            // File size
            writer.Write(10 + body.Length);

            // Body
            writer.Write(body);

            Console.WriteLine($"Wrote Windows XNB: {outputFile}");
            Console.WriteLine($"Size: {10 + body.Length} bytes");
        }
    }
}
