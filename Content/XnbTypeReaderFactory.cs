using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using XnbExtractor.Content;
using XnbExtractor.Content.Reflection;

namespace XnbExtractor.Readers
{
    //class FnaReaderAdapter : IXnbTypeReader
    //{
    //    private readonly Microsoft.Xna.Framework.Content.ContentTypeReader _reader;

    //    public object Read(XnbContentReader reader)
    //    {
    //        // Adapter between your reader and FNA's reader.
    //    }
    //}

    public class ReflectionContentReader
    {
        private XnbContentReader _reader;

        public ReflectionContentReader(XnbContentReader reader)
        {
            _reader = reader;
        }

        public int ReadInt32()
            => _reader.ReadInt32();


        public float ReadSingle()
            => _reader.ReadSingle();


        public bool ReadBoolean()
            => _reader.ReadBoolean();


        public string ReadString()
            => _reader.ReadString();


        public byte ReadByte()
            => _reader.ReadByte();


        public byte[] ReadBytes(int count)
            => _reader.ReadBytes(count);


        public T ReadObject<T>()
        {
            // your type reader dispatch here
            return default;
        }

    }

    public static class ReflectionReaderFactory
    {
        private static readonly Dictionary<string, Type> assembly_readers = new();

        public static void RegisterAssemblyReaders(Assembly asm)
        {
            foreach (Type type in asm.GetTypes())
            {
                Type? baseType = type.BaseType;

                if (baseType == null || !baseType.IsGenericType)
                    continue;

                if (baseType.GetGenericTypeDefinition().Name != "ContentTypeReader`1")
                    continue;

                string name = type.FullName + "," + type.Assembly.GetName().Name;

                assembly_readers[name] = type;

                Console.WriteLine($"Registered {name}");
            }
        }

        public static object ReadByType(string typeName, XnbContentReader reader)
        {
            Console.WriteLine($"Assembly Factory lookup: {typeName}");

            if (!assembly_readers.TryGetValue(typeName, out var readerType))
                throw new NotSupportedException(typeName);

            return InvokeReader(readerType, reader);
        }

        private static object InvokeReader(Type readerType, XnbContentReader reader)
        {
            Console.WriteLine($"Reader type: {readerType.FullName}");

            var methods = readerType.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                if (method.Name != "Read")
                    continue;

                var parameters = method.GetParameters();

                if (parameters.Length != 2)
                    continue;

                Console.WriteLine($"Candidate: {method}");

                if (parameters[0].ParameterType != typeof(XnbContentReader))
                    continue;

                if (parameters[1].ParameterType == typeof(object))
                    continue;

                Console.WriteLine($"Selected: {method}");

                var instance = Activator.CreateInstance(readerType);

                return method.Invoke(instance, new object[] { reader, null });
            }

            throw new MissingMethodException(
                $"No suitable Read(XnbContentReader,T) method found on {readerType.FullName}");
        }
    }


    public static class XnbTypeReaderFactory
    {
        private static readonly Dictionary<string, IXnbTypeReader> Readers = [];

        public static void Register(string typeName, IXnbTypeReader reader)
        {
            Readers[typeName] = reader;
        }

        private static object ReadList(string type, XnbContentReader reader)
        {
            int count = reader.ReadInt32();

            Console.WriteLine($"List count = {count}");
            Console.WriteLine($"List type  = {type}");

            if (type.Contains("Microsoft.Xna.Framework.Rectangle"))
            {
                var list = new List<Rectangle>(count);

                for (int i = 0; i < count; i++)
                    list.Add(reader.ReadRectangle());

                return list;
            }

            if (type.Contains("System.Char"))
            {
                var list = new List<char>(count);

                for (int i = 0; i < count; i++)
                    list.Add(reader.ReadChar());

                return list;
            }

            if (type.Contains("Microsoft.Xna.Framework.Vector3"))
            {
                var list = new List<Vector3>(count);

                for (int i = 0; i < count; i++)
                    list.Add(reader.ReadVector3());

                return list;
            }

            throw new NotSupportedException(
                $"Unsupported ListReader: {type}");
        }

        private static object ReadArray(string type, XnbContentReader reader)
        {
            int count = reader.ReadInt32();

            if (type.Contains("System.String"))
            {
                string[] array = new string[count];

                for (int i = 0; i < count; i++)
                    array[i] = (string)reader.ReadObject()!;

                return array;
            }

            if (type.Contains("System.Int32"))
            {
                int[] array = new int[count];

                for (int i = 0; i < count; i++)
                    array[i] = reader.ReadInt32();

                return array;
            }

            throw new NotSupportedException($"Unsupported ArrayReader: {type}");
        }
        public static object ReadByType(string type, XnbContentReader reader)
        {
            Console.WriteLine($"Factory lookup: '{type}'");

            string shortName = type.Split(',')[0].Trim();

            if (Readers.TryGetValue(type, out var readerFactory))
                return readerFactory.Read(reader);

            if (Readers.TryGetValue(shortName, out readerFactory))
                return readerFactory.Read(reader);

            if (shortName.StartsWith("Microsoft.Xna.Framework.Content.ArrayReader`1"))
                return ReadArray(type, reader);

            if (shortName.StartsWith("Microsoft.Xna.Framework.Content.ListReader`1"))
                return ReadList(type, reader);

            if (shortName.StartsWith("isdf.Runtime."))
                return RuntimeReaderFactory.ReadType(reader, type);

            return ReflectionReaderFactory.ReadByType(type, reader);
        }

    }
}