using System;
using XnbExtractor.Content;

namespace XnbExtractor.Readers
{
    public class XnbTEventScriptSheetReader : IXnbTypeReader
    {
        public object Read(XnbContentReader reader)
        {
            Console.WriteLine("Reading TEventScriptSheet");
            Console.WriteLine(
    $"TEventScriptSheetReader position: {reader.Position}"
);
            var bmpNames = reader.ReadObject<string[]>();
             Console.WriteLine($"After bmpNames: {reader.Position}");
            var gridW = reader.ReadObject<int[]>();
            
            Console.WriteLine($"After gridW: {reader.Position}");
            var gridH = reader.ReadObject<int[]>();
            Console.WriteLine($"After gridH: {reader.Position}");
            var names = reader.ReadObject<string[]>();
           
            
            Console.WriteLine($"After names: {reader.Position}");
            var header = new TEventScriptHeader(
                bmpNames,
                gridW,
                gridH,
                names
            );

            Console.WriteLine($"BmpNames: {bmpNames.Length}");
            Console.WriteLine($"GridW: {gridW.Length}");
            Console.WriteLine($"GridH: {gridH.Length}");
            Console.WriteLine($"Names: {names.Length}");

            int lineCount = reader.ReadInt32();

            Console.WriteLine($"Line count: {lineCount}");

            var lines = new TEventScriptLine[lineCount];

            for (int i = 0; i < lineCount; i++)
            {
                Console.WriteLine($"Line {i}");

                lines[i] = new TEventScriptLine(
                    reader.ReadObject<string[]>(),
                    reader.ReadObject<int[]>(),
                    reader.ReadObject<int[]>(),
                    reader.ReadObject<int[]>(),
                    reader.ReadObject<int[]>(),
                    reader.ReadObject<string>()
                );
            }

            return new XnbTEventScriptSheet(header, lines);
        }
    }

    public class XnbTEventScriptSheet
    {
        public TEventScriptHeader header { get; }
        public TEventScriptLine[] lines { get; }

        public XnbTEventScriptSheet(
            TEventScriptHeader Header,
            TEventScriptLine[] Lines)
        {
            header = Header;
            lines = Lines;
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== Event Script Sheet ===");

            sb.AppendLine("Header:");
            sb.AppendLine($"Bitmap Names: {string.Join(", ", header.BmpNames)}");
            sb.AppendLine($"Names: {string.Join(", ", header.Names)}");

            sb.AppendLine();
            sb.AppendLine($"Lines: {lines.Length}");

            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendLine($"--- Line {i} ---");

                sb.AppendLine(
                    $"Strings: {string.Join(", ", lines[i].Strings ?? Array.Empty<string>())}"
                );

                sb.AppendLine(
                    $"Ints1: {string.Join(", ", lines[i].Ints1 ?? Array.Empty<int>())}"
                );

                sb.AppendLine(
                    $"Ints2: {string.Join(", ", lines[i].Ints2 ?? Array.Empty<int>())}"
                );

                sb.AppendLine(
                    $"Ints3: {string.Join(", ", lines[i].Ints3 ?? Array.Empty<int>())}"
                );

                sb.AppendLine(
                    $"Ints4: {string.Join(", ", lines[i].Ints4 ?? Array.Empty<int>())}"
                );

                sb.AppendLine($"Text: {lines[i].Text}");
            }

            return sb.ToString();
        }
    }
    public class TEventScriptLine
    {
        public string[] Strings { get; }
        public int[] Ints1 { get; }
        public int[] Ints2 { get; }
        public int[] Ints3 { get; }
        public int[] Ints4 { get; }
        public string Text { get; }

        public TEventScriptLine(
            string[] strings,
            int[] ints1,
            int[] ints2,
            int[] ints3,
            int[] ints4,
            string text)
        {
            Strings = strings;
            Ints1 = ints1;
            Ints2 = ints2;
            Ints3 = ints3;
            Ints4 = ints4;
            Text = text;
        }
    }
    public class TEventScriptHeader
    {
        public string[]? BmpNames { get; }
        public int[]? GridW { get; }
        public int[]? GridH { get; }
        public string[]? Names { get; }

        public TEventScriptHeader(
            string[]? bmpNames,
            int[]? gridW,
            int[]? gridH,
            string[]? names)
        {
            BmpNames = bmpNames;
            GridW = gridW;
            GridH = gridH;
            Names = names;
        }
    }
}