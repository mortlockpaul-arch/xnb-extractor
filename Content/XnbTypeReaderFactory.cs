using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using XnbExtractor.Content;
using XnbExtractor.Content.Readers;

namespace XnbExtractor.Readers
{
    public static class XnbTypeReaderFactory
    {
        private static readonly Dictionary<string, IXnbTypeReader> Readers = new()
        {
            {
                "isdf.Runtime.TCSVtoArrayReader,isdf.Runtime",
                new XnbTCSVtoArrayReader()
            },
            {
                "isdf.Runtime.TEdgePalReader,isdf.Runtime",
                new XnbTEdgePalReader()
            },
            {
                "Microsoft.Xna.Framework.Content.StringReader",
                new XnbStringReader()
            },
            {
                "Microsoft.Xna.Framework.Content.Int32Reader",
                new XnbInt32Reader()
            },
            {
                "isdf.Runtime.TEventScriptSheetReader,isdf.Runtime",
                new XnbTEventScriptSheetReader()
            }
        };

        public static object ReadByType(
            string type,
            XnbContentReader reader)
        {
            Console.WriteLine($"Factory lookup: '{type}'");

            if (Readers.TryGetValue(type, out var customReader))
                return customReader.Read(reader);

            string shortName = type.Split(',')[0].Trim();

            if (Readers.TryGetValue(shortName, out customReader))
                return customReader.Read(reader);

            if (type.Contains("Texture2DReader"))
                return XnbTexture2DReader.Parse(reader);

            if (type.Contains("ModelReader"))
                return XnbModelReader.Read(reader);

            if (type.Contains("ArrayReader"))
            {
                if (type.Contains("System.String"))
                {
                    return new XnbArrayReader<string>(
                        r => (string)r.ReadObject()
                    ).Read(reader);
                }

                if (type.Contains("System.Int32"))
                {
                    return new XnbArrayReader<int>(
                        r => r.ReadInt32()
                    ).Read(reader);
                }
            }
            if (type.Contains("Int32Reader"))
            {
                return reader.ReadInt32();
            }
            if (type.Contains("StringReader"))
                return reader.ReadString();

            if (type.Contains("Int32Reader"))
                return reader.ReadInt32();

            //Console.WriteLine(
            //    $"Factory lookup: '{type}'");
            //Console.WriteLine(
            //    $"Contains ArrayReader: {type.Contains("ArrayReader")}"
            //);

            //Console.WriteLine(
            //    $"Contains System.String: {type.Contains("System.String")}"
            //);
            //IXnbTypeReader typeReader = null;
            //// 1. Custom / registered readers
            //if (Readers.TryGetValue(type, out var customReader))
            //{
            //    Console.WriteLine($"Using registered reader: {type}");
            //    return customReader.Read(reader);
            //}


            //// 2. Strip assembly name and try again
            //string shortName = type.Split(',')[0].Trim();

            //if (Readers.TryGetValue(shortName, out customReader))
            //{
            //    Console.WriteLine($"Using registered reader: {shortName}");
            //    return customReader.Read(reader);
            //}


            //// 3. Built-in XNA readers
            //if (type.Contains("Texture2DReader"))
            //{
            //    Console.WriteLine("Using Texture2D reader");
            //    return XnbTexture2DReader.Parse(reader);
            //}

            //if (type.Contains("ModelReader"))
            //{
            //    Console.WriteLine("Using Model reader");
            //    return XnbModelReader.Read(content, reader);
            //}
            //if (type.Contains("ArrayReader"))
            //{
            //    if (type.Contains("System.String"))
            //    {
            //        Console.WriteLine("Creating String ArrayReader");

            //        return new XnbArrayReader<string>(
            //            r => r.ReadString()
            //        ).Read(reader);
            //    }

            //    if (type.Contains("System.Int32"))
            //    {
            //        Console.WriteLine("Creating Int32 ArrayReader");

            //        return new XnbArrayReader<int>(
            //            r => r.ReadInt32()
            //        ).Read(reader);
            //    }
            //}
            if (type.Contains("isdf.Runtime.TBmp256Reader,isdf.Runtime"))
            {
                return new TBmp256Reader();
            }
            throw new NotSupportedException(
                $"Unsupported content reader: {type}");
        }
    }
}