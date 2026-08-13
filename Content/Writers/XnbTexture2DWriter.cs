using System;
using XnbExtractor.Content;
using XnbExtractor.Models;

namespace XnbExtractor.Content.Writers
{
    public static class XnbTexture2DWriter
    {
        public static void Write(
            XnbContentWriter writer,
            XnbTexture texture)
        {
            if (!texture.WindowsFormat.HasValue)
            {
                throw new InvalidOperationException(
                    $"No Windows format for texture " +
                    $"(detected={texture.Format}, raw={texture.RawFormat})");
            }

            writer.WriteInt32((int)texture.WindowsFormat.Value);
            writer.WriteInt32(texture.Width);
            writer.WriteInt32(texture.Height);
            writer.WriteInt32(texture.MipCount);

            if (texture.MipData.Count != texture.MipCount)
            {
                throw new InvalidOperationException(
                    $"Mip count mismatch: expected {texture.MipCount}, " +
                    $"got {texture.MipData.Count}");
            }

            foreach (byte[] mip in texture.MipData)
            {
                writer.WriteInt32(mip.Length);
                writer.WriteBytes(mip);
            }
        }
    }
}