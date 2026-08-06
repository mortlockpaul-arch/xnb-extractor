using isdf.Runtime;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using XnbExtractor.Content;
using XnbExtractor.Content.Readers;
using XnbExtractor.Models;
using XnbExtractor.Readers;
using XnbExtractor.Writers;
using XnbExtractor.Xnb;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

using RuntimeTBmp256 = isdf.Runtime.TBmp256;
using ExtractorTBmp256 = XnbExtractor.Models.XnbTBmp256;

using RuntimeTEdgePal = isdf.Runtime.TEdgePal;
using Runtime = isdf.Runtime;
using Models = XnbExtractor.Models;
using RuntimeEventScriptSheet = isdf.Runtime.TEventScriptSheet;

namespace XnbExtractor
{
    internal static class Program
    {
        static string inputRoot;
        static string outputRoot;

        static void Main(string[] args)
        {
            var logfile = new StreamWriter("xnbextractor.log")
            {
                AutoFlush = true
            };

            Console.SetOut(new TeeTextWriter(Console.Out, logfile));
            Console.SetError(new TeeTextWriter(Console.Error, logfile));

            Console.WriteLine("XNB Extractor");

            string input = "";
            string output = "";
            bool loader = false;
            bool parser = false;
            bool dds = false;
            bool overwrite = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--input":
                        input = args[++i];
                        break;

                    case "--output":
                        output = args[++i];
                        break;

                    case "--loader":
                        loader = true;
                        break;

                    case "--parser":
                        parser = true;
                        break;

                    case "--dds":
                        dds = true;
                        break;

                    case "--overwrite":
                        overwrite = true;
                        break;
                }
            }

            input = args.Length > 0
                ? args[0]
                : @"C:\PycharmProjects\xenia-game-manager\src\downloads\XBLIG\Downtown SMASH Dodgeball! (World) (XBLIG)\584E07D2\00000002\extracted\584E07D1\";

            // add this
            inputRoot = Directory.Exists(input) ? input : Path.GetDirectoryName(input) ?? input;

            // set an output root (adjust as you prefer)
            outputRoot = Path.Combine(Directory.GetCurrentDirectory(), "output");

            bool runLoaderTest = args.Contains("--loader");
            bool runParserTest = args.Contains("--parser");

            int succeeded = 0;
            int failed = 0;

            if (!runLoaderTest && !runParserTest)
            {
                runLoaderTest = false;
                runParserTest = true;
            }

            if (System.IO.File.Exists(input))
            {
                if (!input.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("FAILED| Input file is not an XNB.");
                    return;
                }

                ProcessFile(input, runLoaderTest, runParserTest);
                Environment.Exit(failed == 0 ? 0 : 1);
            }

            if (Directory.Exists(input))
            {
                var dll_files = ListDllFiles(input);

                if (dll_files.Count > 0)
                {
                    Console.WriteLine($"Found {dll_files.Count} DLL files.");
                    foreach (var dll in dll_files)
                    {
                        Console.WriteLine($"Loading DLL: {dll.FullName}");
                        try
                        {
                            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                            {
                                if (args.Name.StartsWith("Microsoft.Xna.Framework"))
                                {
                                    return Assembly.LoadFrom(
                                        @"C:\PycharmProjects\xenia-game-manager\src\References\v3.1\References\Xbox360\Microsoft.Xna.Framework.dll");
                                }

                                return null;
                            };
                            XnaAssemblyResolver.Install();
                            Assembly asm = Assembly.LoadFrom(dll.FullName);
                            foreach (Type t in asm.GetTypes())
                            {
                                Console.WriteLine(t.FullName);

                                Type? baseType = t.BaseType;

                                if (baseType == null)
                                    continue;

                                if (!baseType.IsGenericType)
                                    continue;

                                if (baseType.GetGenericTypeDefinition().Name != "ContentTypeReader`1")
                                    continue;

                                Console.WriteLine($"Reader : {t.FullName}");

                                Type model = baseType.GetGenericArguments()[0];

                                Console.WriteLine($"    Reads : {model.FullName}");
                            }
                            Console.WriteLine($"Loaded DLL: {dll.FullName}");
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            Console.WriteLine($"ReflectionTypeLoadException loading {dll.Name}");

                            foreach (var loaderEx in ex.LoaderExceptions)
                            {
                                Console.WriteLine("--------------------------------");
                                Console.WriteLine(loaderEx.GetType().FullName);
                                Console.WriteLine(loaderEx.Message);

                                if (loaderEx is FileNotFoundException fnf)
                                    Console.WriteLine($"Missing: {fnf.FileName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No DLL files found.");
                }

                var files = ListXnbFiles(input);

                if (files.Count == 0)
                {
                    Console.WriteLine("FAILED| No XNB files found.");
                    return;
                }

                Console.WriteLine($"Found {files.Count} XNB files.");

                foreach (var file in files)
                {
                    ProcessFile(file.FullName, runLoaderTest, runParserTest);
                }

                Environment.Exit(failed == 0 ? 0 : 1);
            }

            Console.WriteLine($"FAILED| Path not found: {input}");
        }

        private static readonly Dictionary<string, string> XnaMap =
            new()
            {
                ["Microsoft.Xna.Framework"] =
                    @"v3.1\References\Xbox360\Microsoft.Xna.Framework.dll",

                ["Microsoft.Xna.Framework.Game"] =
                    @"v3.1\References\Xbox360\Microsoft.Xna.Framework.Game.dll",

                ["Microsoft.Xna.Framework.Graphics"] =
                    @"v3.1\References\Xbox360\Microsoft.Xna.Framework.Graphics.dll",

                ["Microsoft.Xna.Framework.Content"] =
                    @"v3.1\References\Xbox360\Microsoft.Xna.Framework.Content.dll"
            };

        public static class XnaAssemblyResolver
        {
            private static readonly string ReferenceRoot =
                @"C:\PycharmProjects\xenia-game-manager\src\References";


            public static void Install()
            {
                AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            }


            private static Assembly Resolve(
                object sender,
                ResolveEventArgs args)
            {
                AssemblyName requested =
                    new AssemblyName(args.Name);


                Console.WriteLine(
                    $"XNA Resolve: {requested.FullName}");


                if (requested.Name == "Microsoft.Xna.Framework")
                {
                    string path =
                        Path.Combine(
                            ReferenceRoot,
                            "v3.1",
                            "References",
                            "Xbox360",
                            "Microsoft.Xna.Framework.dll");


                    if (File.Exists(path))
                    {
                        return Assembly.LoadFrom(path);
                    }
                }


                return null;
            }
        }
        static void ProcessFile(string inputFile, bool runLoaderTest, bool runParserTest)
        {
            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"Input: {inputFile}");

            string relative = Utilities.GetRelativePath(inputRoot, inputFile);

            string outputFile = Path.Combine(
                outputRoot,
                Path.ChangeExtension(relative, ".dds"));

            if (runLoaderTest)
                TestLoaderGame(inputFile);

            if (runParserTest)
                ParseXnb(inputFile, outputFile);
        }

        static List<FileInfo> ListDllFiles(string root, int? limit = null)
        {
            var files = Directory.GetFiles(
                    root,
                    "*.dll",
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

        public class LoadedAsset
        {
            public object Asset { get; init; }
            public TargetPlatform Platform { get; init; }
        }

        public static LoadedAsset LoadAsset(string inputFile)
        {
            using var xnb = new XnbFile(inputFile);

            var decompressed = xnb.Decompress();

            var content = XnbContentReader.ParseContent(decompressed);

            using var reader = new XnbContentReader(
                new MemoryStream(decompressed),
                content,
                xnb.Header.Platform);

            reader.Seek(content.DataPosition);

            return new LoadedAsset
            {
                Asset = XnbTypeReaderFactory.ReadByType(
                        content.PrimaryReaderType,
                        reader),
                Platform = xnb.Header.Platform
            };
        }

        //public static void ExportAsset(object asset, string outputFile)
        //{

        //    switch (asset)
        //    {
        //        case string[] strings:
        //            {
        //                var textFile = outputFile + ".txt";

        //                Directory.CreateDirectory(
        //                    Path.GetDirectoryName(textFile)!
        //                );

        //                System.IO.File.WriteAllLines(textFile, strings);
        //                break;
        //            }
        //        case XnbTEventScriptSheet sheet:
        //            {
        //                var textFile = Path.ChangeExtension(outputFile, ".txt");

        //                Directory.CreateDirectory(
        //                    Path.GetDirectoryName(textFile)!
        //                );

        //                System.IO.File.WriteAllText(
        //                    textFile,
        //                    sheet.ToString()
        //                );
        //                ExportEventScript(sheet, outputFile);
        //                break;
        //            }
        //        case XnbTBmp256 bmp:
        //            {
        //                Console.WriteLine($"Extracted TBmp256 {bmp.Width}x{bmp.Height}");

        //                File.WriteAllBytes(
        //                    Path.ChangeExtension(outputFile, ".palidx"),
        //                    bmp.PalIdx
        //                );

        //                var tPalARGB = new uint[256];
        //                var edgePal = new TEdgePal(tPalARGB);
        //                var manager = new TEdgePalManager(edgePal);

        //                var palette = manager.GetPalAll(
        //                    0, 0, 0, 0, 0, 0, 0, 0, 0);

        //                TBmp256PngWriter.Write(
        //                    bmp,
        //                    palette,
        //                    Path.ChangeExtension(outputFile, ".png")
        //                );

        //                break;
        //            }
        //        default:
        //            System.IO.File.WriteAllText(
        //                outputFile + ".txt",
        //                asset.ToString()
        //            );

        //            Console.WriteLine(
        //                $"Exported unknown asset {asset.GetType()}"
        //            );
        //            break;

        //        case CsvTable[] tables:
        //            for (int t = 0; t < tables.Length; t++)
        //            {
        //                Console.WriteLine($"=== Table {t} ===");

        //                foreach (var s in tables[t].Strings)
        //                    Console.WriteLine(s);

        //                Console.WriteLine("Indices:");
        //                Console.WriteLine(string.Join(", ", tables[t].Indices));
        //            }
        //            break;
        //        case XnbModel model:
        //            Console.WriteLine("Model loaded!");
        //            Console.WriteLine($"Meshes: {model.Meshes.Count}");
        //            break;

        //        case EdgePal palette:
        //            Console.WriteLine("Palette loaded!");
        //            Console.WriteLine($"Colours: {palette.Colors.Length}");

        //            using (var image = new Bitmap(256, 256))
        //            using (var g = Graphics.FromImage(image))
        //            {
        //                for (int i = 0; i < palette.Colors.Length; i++)
        //                {
        //                    int x = (i % 16) * 16;
        //                    int y = (i / 16) * 16;

        //                    uint color = palette.Colors[i];

        //                    byte a = (byte)(color >> 24);
        //                    byte r = (byte)(color >> 16);
        //                    byte gValue = (byte)(color >> 8);
        //                    byte b = (byte)color;

        //                    using var brush = new SolidBrush(
        //                        Color.FromArgb(a, r, gValue, b));

        //                    g.FillRectangle(
        //                        brush,
        //                        x,
        //                        y,
        //                        16,
        //                        16);
        //                }

        //                string pngFile = Path.ChangeExtension(outputFile, ".png");
        //                Console.WriteLine($"Saving palette preview: {pngFile}");
        //                string? dir = Path.GetDirectoryName(pngFile);

        //                if (!string.IsNullOrEmpty(dir))
        //                {
        //                    Directory.CreateDirectory(dir);
        //                }
        //                image.Save(
        //                    pngFile,
        //                    System.Drawing.Imaging.ImageFormat.Png);

        //                Console.WriteLine($"Palette preview written: {pngFile}");
        //            }

        //            break;

        //        case XnbTexture texture:
        //            Console.WriteLine("Texture loaded!");
        //            if (texture.WindowsFormat.HasValue)
        //                Console.WriteLine($"Format: {texture.WindowsFormat.Value}");
        //            else if (texture.XboxFormat.HasValue)
        //                Console.WriteLine($"Format: {texture.XboxFormat.Value}");
        //            else
        //                Console.WriteLine($"Raw Format: {texture.RawFormat}");
        //            Console.WriteLine($"{texture.Width}x{texture.Height}");
        //            Console.WriteLine($"MipCount: {texture.MipCount}");

        //            for (int i = 0; i < texture.MipData.Count; i++)
        //            {
        //                Console.WriteLine($"Mip {i}: {texture.MipData[i].Length} bytes");
        //            }
        //            Console.WriteLine($"Platform   : {reader.Platform}");
        //            Console.WriteLine($"Raw format : {texture.RawFormat}");

        //            if (asset .Platform == TargetPlatform.Xbox360)
        //                Console.WriteLine($"XboxFormat : {texture.XboxFormat}");
        //            else
        //                Console.WriteLine($"WinFormat  : {texture.WindowsFormat}");
        //            DdsWriter.Write(texture, outputFile);
        //            break;
        //    }
        //}

        public static void ExportAsset(object asset,string inputFile, string outputFile)
        {
            switch (asset)
            {
                case string[] strings:
                    ExportStrings(strings, outputFile);
                    break;

                case XnbTEventScriptSheet sheet:
                    ExportEventScriptSheet(sheet, outputFile);
                    break;

                case XnbTBmp256 bmp:
                    ExportBmp256(bmp, inputFile, outputFile);
                    break;

                case CsvTable[] tables:
                    ExportCsvTables(tables);
                    break;

                case XnbModel model:
                    ExportModel(model);
                    break;

                case EdgePal palette:
                    ExportPalette(palette, outputFile);
                    break;

                case XnbTexture texture:
                    ExportTexture(texture, outputFile);
                    break;

                default:
                    ExportUnknown(asset, outputFile);
                    break;
            }
        }

        private static void ExportStrings(string[] strings, string outputFile)
        {
            var textFile = outputFile + ".txt";

            Directory.CreateDirectory(
                Path.GetDirectoryName(textFile)!);

            File.WriteAllLines(textFile, strings);
        }

        private static void ExportEventScriptSheet(XnbTEventScriptSheet sheet, string outputFile)
        {
            var textFile = Path.ChangeExtension(outputFile, ".txt");

            Directory.CreateDirectory(
                Path.GetDirectoryName(textFile)!);

            File.WriteAllText(textFile, sheet.ToString());

            ExportEventScript(sheet, outputFile);
        }

        private static void ExportBmp256(XnbTBmp256 bmp, string inputFile, string outputFile)
        {
            DirectoryInfo dir = new FileInfo(inputFile).Directory!;

            while (dir != null && dir.Name != "Content")
                dir = dir.Parent!;

            string contentRoot = dir.FullName;

            var palettes = new List<TEdgePal>();

            for (int i = 0; ; i++)
            {
                string palFile = Path.Combine(
                    contentRoot,
                    "data",
                    $"pal{i}.xnb");

                if (!File.Exists(palFile))
                    break;

                palettes.Add(
                    (TEdgePal)LoadAsset(palFile).Asset);
            }

            var manager = new TEdgePalManager(palettes.ToArray());

            uint[] palette = manager.GetPalAll(
                    0, // eye
                    0, // skin
                    0, // hair
                    0, // etc
                    0, // pants A
                    0, // pants B
                    0, // pants C
                    0, // shirt
                    0  // shoes
                );

            Console.WriteLine(
                $"Extracted TBmp256 {bmp.Width}x{bmp.Height}");

            File.WriteAllBytes(
                Path.ChangeExtension(outputFile, ".palidx"),
                bmp.PalIdx);

            TBmp256PngWriter.Write(
                bmp,
                palette,
                Path.ChangeExtension(outputFile, ".png"));
        }

        private static void ExportCsvTables(CsvTable[] tables)
        {
            for (int t = 0; t < tables.Length; t++)
            {
                Console.WriteLine($"=== Table {t} ===");

                foreach (var s in tables[t].Strings)
                    Console.WriteLine(s);

                Console.WriteLine("Indices:");
                Console.WriteLine(string.Join(", ", tables[t].Indices));
            }
        }

        private static void ExportModel(XnbModel model)
        {
            Console.WriteLine("Model loaded!");
            Console.WriteLine($"Meshes: {model.Meshes.Count}");
        }

        private static void ExportPalette(EdgePal palette, string outputFile)
        {
            Console.WriteLine("Palette loaded!");
            Console.WriteLine($"Colours: {palette.Colors.Length}");

            using var image = new Bitmap(256, 256);
            using var g = Graphics.FromImage(image);

            for (int i = 0; i < palette.Colors.Length; i++)
            {
                int x = (i % 16) * 16;
                int y = (i / 16) * 16;

                uint color = palette.Colors[i];

                using var brush = new SolidBrush(
                    Color.FromArgb(
                        (byte)(color >> 24),
                        (byte)(color >> 16),
                        (byte)(color >> 8),
                        (byte)color));

                g.FillRectangle(brush, x, y, 16, 16);
            }

            string pngFile = Path.ChangeExtension(outputFile, ".png");

            Directory.CreateDirectory(
                Path.GetDirectoryName(pngFile)!);

            image.Save(
                pngFile,
                System.Drawing.Imaging.ImageFormat.Png);
        }

        private static void ExportTexture(XnbTexture texture, string outputFile)
        {
            Console.WriteLine("Texture loaded!");

            Console.WriteLine($"{texture.Width}x{texture.Height}");
            Console.WriteLine($"MipCount: {texture.MipCount}");

            DdsWriter.Write(texture, outputFile);
        }

        private static void ExportUnknown(object asset, string outputFile)
        {
            File.WriteAllText(
                outputFile + ".txt",
                asset.ToString());

            Console.WriteLine(
                $"Exported unknown asset {asset.GetType()}");
        }


        static void ParseXnb(string inputFile, string outputFile)
        {
            try
            {
                var asset = LoadAsset(inputFile);

                Console.WriteLine(asset.GetType());

                ExportAsset(asset, inputFile, outputFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        //static void ParseXnb(string inputFile, string outputFile)
        //{
        //    Console.WriteLine();
        //    Console.WriteLine("=== XNB Parser Test ===");

        //    try
        //    {
        //        using var xnb = new XnbFile(inputFile);

        //        var decompressedData = xnb.Decompress();
        //        Console.WriteLine(BitConverter.ToString(decompressedData, 0, 32));

        //        System.IO.File.WriteAllBytes("decompressed.bin", decompressedData);
        //        Console.WriteLine($"Decompressed size: {decompressedData.Length}");

        //        XnbContent content = XnbContentReader.ParseContent(decompressedData);

        //        //content.Platform = xnb.Header.Platform;

        //        Console.WriteLine($"Readers: {content.Readers.Count}");

        //        using var reader = new XnbContentReader(
        //            new MemoryStream(decompressedData),
        //            content,
        //            xnb.Header.Platform);

        //        reader.Seek(content.DataPosition);

        //        for (int i = 0; i < 16; i++)
        //        {
        //            Console.Write($"{reader.ReadByte():X2} ");
        //        }
        //        Console.WriteLine();

        //        reader.Seek(content.DataPosition);

        //        Console.WriteLine($"Primary reader: {content.PrimaryReaderType}");

        //        var asset = XnbTypeReaderFactory.ReadByType(
        //            content.PrimaryReaderType,
        //            reader
        //        );

        //        Console.WriteLine(asset?.GetType());



        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Parser failed: {ex}");
        //    }
        //}

        static void ExportEventScript(XnbTEventScriptSheet sheet, string output)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== TEventScriptSheet ===");
            sb.AppendLine();

            sb.AppendLine("Lines:");

            for (int i = 0; i < sheet.lines.Length; i++)
            {
                var line = sheet.lines[i];

                sb.AppendLine($"--- Line {i} ---");

                if (line.Strings != null)
                {
                    sb.AppendLine(
                        "Strings: " +
                        string.Join(", ", line.Strings)
                    );
                }

                if (line.Ints1 != null)
                    sb.AppendLine(
                        "Ints1: " +
                        string.Join(", ", line.Ints1)
                    );

                if (line.Ints2 != null)
                    sb.AppendLine(
                        "Ints2: " +
                        string.Join(", ", line.Ints2)
                    );

                if (line.Ints3 != null)
                    sb.AppendLine(
                        "Ints3: " +
                        string.Join(", ", line.Ints3)
                    );

                if (line.Ints4 != null)
                    sb.AppendLine(
                        "Ints4: " +
                        string.Join(", ", line.Ints4)
                    );

                sb.AppendLine(
                    "Text: " + line.Text
                );
            }

            File.WriteAllText(
                output + ".txt",
                sb.ToString()
            );
        }

        public sealed class TeeTextWriter : TextWriter
        {
            private readonly TextWriter[] writers;

            public TeeTextWriter(params TextWriter[] writers)
            {
                this.writers = writers;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void WriteLine(string? value)
            {
                foreach (var writer in writers)
                    writer.WriteLine(value);
            }

            public override void Write(char value)
            {
                foreach (var writer in writers)
                    writer.Write(value);
            }

            public override void Flush()
            {
                foreach (var writer in writers)
                    writer.Flush();
            }
        }
    }
}