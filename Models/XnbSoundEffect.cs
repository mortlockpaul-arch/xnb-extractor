using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnbExtractor.Models
{
    public enum AudioFormat
    {
        Unknown,
        PCM,
        ADPCM,
        XMA2
    }

    public class XnbSoundEffect
    {
        public ushort FormatTag;
        public ushort Channels;
        public uint SampleRate;
        public uint AverageBytesPerSecond;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        public uint SampleCount;
        public byte[] ExtraData;

        public byte[] AudioData;

        public int LoopStart;
        public int LoopLength;

        public uint DurationMs;

        public AudioFormat Format
        {
            get
            {
                return FormatTag switch
                {
                    0x0001 => AudioFormat.PCM,
                    0x0002 => AudioFormat.ADPCM,
                    0x0166 => AudioFormat.XMA2,
                    _ => AudioFormat.Unknown
                };
            }
        }
    }
}
