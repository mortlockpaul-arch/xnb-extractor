using System;
using XnbExtractor.Models;

namespace XnbExtractor.Content.Readers
{
    public class XnbEffectReader : IXnbTypeReader
    {
        public object Read(XnbContentReader reader)
        {
            int length = reader.ReadInt32();

            if (length < 0 || length > reader.Remaining)
            {
                throw new InvalidOperationException(
                    $"Invalid effect length {length}, " +
                    $"remaining bytes: {reader.Remaining}");
            }

            var effect = new XnbEffect
            {
                Data = reader.ReadBytes(length)
            };

            Console.WriteLine($"Effect data: {effect.Data.Length} bytes");

            return effect;
        }
    }
}