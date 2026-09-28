
using System;
using XnbExtractor.Models;

namespace XnbExtractor.Content.Writers
{
    public static class XnbSoundEffectWriter
    {
        public static void Write(
            XnbContentWriter writer,
            XnbSoundEffect soundEffect)
        {
            ArgumentNullException.ThrowIfNull(soundEffect);

            if (soundEffect.AudioData == null)
                throw new InvalidOperationException("Audio data is missing.");

            if (soundEffect.FormatTag == 0x0166 &&
                soundEffect.ExtraData?.Length != 34)
            {
                throw new InvalidOperationException(
                    $"Invalid XMA2 extension length: {soundEffect.ExtraData?.Length ?? 0}, expected 34.");
            }

            // WAVEFORMATEX
            writer.WriteUInt32(18u + (uint)(soundEffect.ExtraData?.Length ?? 0));
            writer.WriteUInt16(soundEffect.FormatTag);
            writer.WriteUInt16(soundEffect.Channels);
            writer.WriteUInt32(soundEffect.SampleRate);
            writer.WriteUInt32(soundEffect.AverageBytesPerSecond);
            writer.WriteUInt16(soundEffect.BlockAlign);
            writer.WriteUInt16(soundEffect.BitsPerSample);

            byte[] extraData = soundEffect.ExtraData ?? Array.Empty<byte>();

            writer.WriteUInt16((ushort)extraData.Length);
            writer.WriteBytes(extraData);

            // Encoded audio data
            writer.WriteInt32(soundEffect.AudioData.Length);
            writer.WriteBytes(soundEffect.AudioData);

            // Loop metadata
            writer.WriteInt32(soundEffect.LoopStart);
            writer.WriteInt32(soundEffect.LoopLength);
            writer.WriteUInt32(soundEffect.DurationMs);
        }
    }
}