using System;
using System.Collections.Generic;
using XnbExtractor.Content;

namespace XnbExtractor.Content.Readers
{
    internal class ListReader<T> : IXnbTypeReader
    {
        public object Read(XnbContentReader reader)
        {
            int count = reader.ReadInt32();
            var list = new List<T>(count);

            Console.WriteLine($"List<{typeof(T).Name}> count = {count}");

            for (int i = 0; i < count; i++)
            {
                var value = reader.ReadObject<T>();
                list.Add(value!);
            }

            return list;
        }
    }
}