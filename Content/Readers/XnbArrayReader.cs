using System;

namespace XnbExtractor.Content.Readers
{
    public class XnbArrayReader<T>
    {
        private readonly Func<XnbContentReader, T> _reader;

        public XnbArrayReader(Func<XnbContentReader, T> reader)
        {
            _reader = reader;
        }

        public T[] Read(XnbContentReader reader)
        {
            int count = reader.ReadInt32();

            Console.WriteLine($"Array<{typeof(T).Name}> count: {count}");

            var result = new T[count];

            for (int i = 0; i < count; i++)
            {
                Console.WriteLine($"Array element {i}");
                result[i] = _reader(reader);
            }

            return result;
        }
    }
}