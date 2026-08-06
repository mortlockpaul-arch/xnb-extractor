using System;

namespace XnbExtractor.Content.Readers
{
    public class XnbTCSVtoArrayReader : IXnbTypeReader
    {
        public object Read(XnbContentReader reader)
        {
            int count = reader.ReadInt32();
            Console.WriteLine($"Reading {count} CSV tables");
            var result = new CsvTable[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = new CsvTable(
                    reader.ReadObject<string[]>(),
                    reader.ReadObject<int[]>());
            }

            return result;
        }
    }

    public class CsvTable
    {
        public string[] Strings { get; }
        public int[] Indices { get; }

        public CsvTable(string[] strings, int[] indices)
        {
            Strings = strings;
            Indices = indices;
        }
    }
}