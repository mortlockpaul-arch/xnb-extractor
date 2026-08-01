using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XnbExtractor.Content;
using XnbExtractor.Readers;
using XnbExtractor.Writers;
using XnbExtractor.Xnb;

namespace XnbExtractor
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("XNB Extractor");

            string input = args.Length > 0
                ? args[0]
                : @"C:\PycharmProjects\xenia-game-manager\src\downloads\XBLIG\Ambiance (World) (XBLIG)";

            bool runLoaderTest = args.Contains("--loader");
            bool runParserTest = args.Contains("--parser");

            if (!runLoaderTest && !runParserTest)
            {
                runLoaderTest = true;
                runParserTest = true;
            }

            if (File.Exists(input))
            {
                if (!input.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Input file is not an XNB.");
                    return;
                }

                ProcessFile(input, runLoaderTest, runParserTest);
                return;
            }

            if (Directory.Exists(input))
            {
                var files = ListXnbFiles(input);

                if (files.Count == 0)
                {
                    Console.WriteLine("No XNB files found.");
                    return;
                }

                Console.WriteLine($"Found {files.Count} XNB files.");

                foreach (var file in files)
                {
                    ProcessFile(file.FullName, runLoaderTest, runParserTest);
                }

                return;
            }

            Console.WriteLine($"Path not found: {input}");
        }


        static void ProcessFile( string inputFile, bool runLoaderTest, bool runParserTest)
        {
            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"Input: {inputFile}");

            var outputFile = Path.ChangeExtension(inputFile, ".dds");

            if (runLoaderTest)
                TestLoaderGame(inputFile);

            if (runParserTest)
                ParseXnb(inputFile, outputFile);
        }
        static List<FileInfo> ListXnbFiles(string root, int? limit = null)
        {
            var files = Directory.GetFiles(
                    root,
                    "*.xnb",
                    SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.Length);

            if (limit.HasValue)
                files = (IOrderedEnumerable<FileInfo>)files.Take(limit.Value);

            var list = files.ToList();

            int index = 1;

            foreach (var file in list)
            {
                var relative = Utilities.GetRelativePath(root, file.FullName);
                double sizeMb = file.Length / 1024d / 1024d;

                Console.WriteLine(
                    $"{index++,4} | {file.Name,-40} {sizeMb,8:F2} MB | {relative}");
            }

            return list;
        }


        static void TestLoaderGame(string inputFile)
        {
            Console.WriteLine();
            Console.WriteLine("=== LoaderGame Test ===");

            try
            {
                using var loaderGame = new LoaderGame(inputFile);

                Console.WriteLine("LoaderGame loaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoaderGame failed: {ex}");
            }
        }


        static void ParseXnb(string inputFile, string outputFile)
        {
            Console.WriteLine();
            Console.WriteLine("=== XNB Parser Test ===");

            try
            {
                using var xnb = new XnbFile(inputFile);

                var decompressed = xnb.Decompress();
                File.WriteAllBytes("decompressed.bin", decompressed);
                Console.WriteLine($"Decompressed size: {decompressed.Length}");

                var content = Content.XnbContentReader.Parse(decompressed);

                Console.WriteLine($"Readers: {content.Readers.Count}");
                Console.WriteLine($"Primary: {content.PrimaryReaderIndex}");
                Console.WriteLine(content.PrimaryReaderType);


                using var reader = new XnbExtractor.Content.XnbContentReader(
                    new MemoryStream(decompressed));

                reader.Seek(content.DataPosition);

                var asset = ContentReaderFactory.Read(content, reader);


                switch (asset)
                {
                    case XnbModel model:
                        Console.WriteLine("Model loaded!");
                        Console.WriteLine($"Meshes: {model.Meshes.Count}");
                        break;


                    case XnbTexture texture:
                        Console.WriteLine("Texture loaded!");
                        Console.WriteLine($"Format: {texture.Format}");
                        Console.WriteLine($"{texture.Width}x{texture.Height}");
                        Console.WriteLine($"MipCount: {texture.MipCount}");

                        for (int i = 0; i < texture.MipData.Count; i++)
                        {
                            Console.WriteLine(
                                $"Mip {i}: {texture.MipData[i].Length} bytes");
                        }

                        DdsWriter.Write(texture, outputFile);

                        Console.WriteLine("DDS written.");
                        break;


                    default:
                        Console.WriteLine("Unknown asset type.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parser failed: {ex}");
            }
        }
    }
}