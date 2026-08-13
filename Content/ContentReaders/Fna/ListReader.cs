
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;

namespace XnbExtractor.Content.ContentReaders.Fna
{
    internal class ListReader<T> : IXnbTypeReader
    {

        public object Read(XnbContentReader reader)
        {
            int count = reader.ReadInt32();
            List<T> list = null;
            if (list == null)
            {
                list = new List<T>(count);
            }
            for (int i = 0; i < count; i += 1)
            {
                Type objectType = typeof(T);
                if (objectType.IsValueType)
                {
                    list.Add(reader.ReadObject<T>(elementReader));
                }
                else
                {
                    int readerType = reader.Read7BitEncodedInt();
                    list.Add((readerType > 0) ? reader.ReadObject<T>(reader.TypeReaders[readerType - 1]) : default(T));
                }
            }
            return list;
        }

    }
}

