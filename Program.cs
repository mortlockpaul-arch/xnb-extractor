using Monogame_Compression.Content.Readers;
using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using XnbExtractor.Content.Readers;
using XnbExtractor.Readers;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct CONSOLE_FONT_INFOEX
{
    public uint cbSize;
    public uint nFont;
    public short dwFontSizeX;
    public short dwFontSizeY;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string FaceName;
}

namespace XnbExtractor
{

    internal static partial class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool bMaximumWindow, ref CONSOLE_FONT_INFOEX lpConsoleCurrentFontEx);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        static void SetConsoleFont(short size)
        {
            var font = new CONSOLE_FONT_INFOEX
            {
                cbSize = (uint)Marshal.SizeOf<CONSOLE_FONT_INFOEX>(),
                nFont = 0,
                dwFontSizeX = 0,
                dwFontSizeY = size,
                FaceName = "Consolas"
            };

            var handle = GetStdHandle(-11);

            if (!SetCurrentConsoleFontEx(handle, false, ref font))
                Console.WriteLine($"Failed to set console font: {Marshal.GetLastWin32Error()}");
        }

        private static bool scanCs;
        private static bool scanContent;
        private static int? limit;
        private static readonly HashSet<string> excludedFolders = new HashSet<string>(["bin", "obj", "Output"], StringComparer.OrdinalIgnoreCase);

        static void Main(string[] args)
        {
            string logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);

            string logPath = Path.Combine(logDir, "xnbextractor.log");

            var logfile = new StreamWriter(logPath)
            {
                AutoFlush = true
            };

            Console.SetOut(new TeeTextWriter(Console.Out, logfile));
            Console.SetError(new TeeTextWriter(Console.Error, logfile));

            Console.SetWindowSize(160, 40);
            //SetConsoleFont(8);
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);

            string input = "";
            string output = "";
            bool loader = false;
            bool parser = true;
            bool overwrite = false;
            bool compressXnb = false;
            bool convert_to_windows = false;
            bool playaudio = false;
#if DEBUG
            loader = true;
            parser = true;
            limit = null;
            scanCs = true;
            scanContent = true;
            playaudio = false;
            if (string.IsNullOrWhiteSpace(input))
            {
                input = @"C:\source\Indie-Games\indie-game-archive\One Night Two Crazies (World) (XBLIG)\";
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                output = @"C:\source\Indie-Games\indie-game-archive\One Night Two Crazies (World) (XBLIG)\Output";
            }
#endif

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--input":
                        if (++i >= args.Length)
                        {
                            Console.WriteLine("Error: --input requires a path.");
                            PrintHelp();
                            return;
                        }

                        input = args[i];
                        break;

                    case "--output":
                        if (++i >= args.Length)
                        {
                            Console.WriteLine("Error: --output requires a path.");
                            PrintHelp();
                            return;
                        }

                        output = args[i];
                        break;
                    case "--loader": loader = true; break;
                    case "--parser": parser = true; break;
                    case "--extract": extract = true; break;
                    case "--compress": compressXnb = true; break;
                    case "--convert-to-windows": convert_to_windows = true; break;
                    case "--overwrite": overwrite = true; break;
                    case "--playaudio": playaudio = true; break;
                    case "--scan-cs": scanCs = true; break;
                    case "--scan-content": scanContent = true; break;
                    case "--limit":
                        if (++i >= args.Length || !int.TryParse(args[i], out int tempLimit))
                        {
                            Console.WriteLine("Error: --limit requires an integer value.");
                            PrintHelp();
                            return;
                        }
                        limit = tempLimit;
                        break;
                }
            }

            if (args.Contains("--help") || args.Contains("-h"))
            {
                PrintHelp();
                Console.WriteLine();
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return;
            }



            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
            {
                PrintHelp();

                Console.WriteLine();
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return;
            }

            if (!Directory.Exists(output)) Directory.CreateDirectory(output);
            InputRoot = Directory.Exists(input) ? input : Path.GetDirectoryName(input) ?? input;
            OutputRoot = Directory.Exists(output) ? output : Path.GetDirectoryName(output) ?? output;

            if (!Directory.Exists(InputRoot)) Directory.CreateDirectory(InputRoot);

            AnsiConsole.MarkupLine("[bold cyan] XNB Extractor[/]");

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Option")
                .AddColumn("Value");

            table.AddRow("Input", input);
            table.AddRow("Output", output);
            table.AddRow("Input Root", InputRoot);
            table.AddRow("Output Root", OutputRoot);
            table.AddRow("Loader", loader.ToString());
            table.AddRow("Parser", parser.ToString());
            table.AddRow("Compress XNB", compressXnb.ToString());
            table.AddRow("Convert to Windows", convert_to_windows.ToString());
            table.AddRow("Overwrite", overwrite.ToString());
            table.AddRow("Play Audio", playaudio.ToString());
            table.AddRow("Scan Source for Audio", scanCs.ToString());
            table.AddRow("Scan Content for Audio", scanContent.ToString());
            table.AddRow("Limit Content Scan", limit?.ToString() ?? "None");
            AnsiConsole.Write(table);

            RegisterReaders();
            LoadGameDependencies(input);
            int succeeded = 0;
            int failed = 0;
            List<AudioReference> audioReferences = new List<AudioReference>();

            if (scanContent)
            {
                ScanContentForAudio(InputRoot);
                Console.WriteLine("Audio content scan complete.");
            }
            if (scanCs)
            {
                audioReferences = ScanCsForAudio(InputRoot);
                Console.WriteLine("Source code scan complete.");
            }

            ProcessOneOrMoreFiles(input, loader, parser, audioReferences, ref succeeded, ref failed, limit: limit);

            if (playaudio) { XnbCore.PlayExportedAudio(OutputRoot); }

            Console.WriteLine("xnb-extractor complete. Press any key to exit.");
            Console.ReadKey();
            return;
        }

        private static readonly HashSet<string> AudioExtensions =
        [
            ".wav",
            ".xwb",
            ".xsb",
            ".xgs",
            ".mp3",
            ".ogg",
            ".wma",
            ".xma"
        ];

        private static readonly HashSet<string> VgmstreamExtensions =
        [
            ".wav",
            ".xwb",
            ".mp3",
            ".ogg",
            ".wma",
            ".xma"
        ];

        private static void ScanContentForAudio(string root)
        {
            string rootPath = Path.GetFullPath(root);
            string outputPath = Path.GetFullPath(OutputRoot);

            var audioFiles = Directory
                .EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    string fullPath = Path.GetFullPath(f);

                    if (fullPath.StartsWith(
                        outputPath + Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    return AudioExtensions.Contains(
                        Path.GetExtension(f).ToLowerInvariant());
                })
                .ToList();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("File")
                .AddColumn("Status");

            table.AddRow(
                $"Found {audioFiles.Count} audio files",
                "");

            var logPanels = new List<IRenderable>();

            // Only the table is live.
            AnsiConsole.Live(table).Start(ctx =>
            {
                foreach (string file in audioFiles)
                {
                    string name = Path.GetFileName(file);
                    string ext = Path.GetExtension(file).ToLowerInvariant();

                    if (!VgmstreamExtensions.Contains(ext))
                    {
                        table.AddRow(name, "[yellow]Skipped[/]");
                        ctx.Refresh();
                        continue;
                    }

                    table.AddRow(name, "[yellow]Processing...[/]");
                    int row = table.Rows.Count - 1;

                    ctx.Refresh();

                    string log = RunVgmstream(file);

                    bool failed = log.StartsWith(
                        "ERROR:",
                        StringComparison.OrdinalIgnoreCase);

                    table.UpdateCell(
                        row,
                        1,
                        failed
                            ? "[red]Failed[/]"
                            : "[green]Complete[/]");

                    logPanels.Add(
                        new Panel(log)
                            .Header($" vgmstream - {name} ")
                            .Border(BoxBorder.Rounded));

                    ctx.Refresh();
                }
            });

            // Live display has finished.
            // Now render everything normally, so nothing gets clipped.
            AnsiConsole.WriteLine();

            foreach (IRenderable panel in logPanels)
            {
                AnsiConsole.Write(panel);
                AnsiConsole.WriteLine();
            }
        }

        private sealed record AudioReference(
            string Line,
            int LineNumber,
            string Reference,
            string File,
            string Type,
            bool IsAudio,
            bool IsContent);

        private static string EscapeMarkup(string text) => Markup.Escape(text);

        private static List<AudioReference> ScanCsForAudio(string root)
        {
            string rootPath = Path.GetFullPath(root);
            string outputPath = Path.GetFullPath(OutputRoot);

            var contentFiles = Directory
                .EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .Where(f => !Path.GetRelativePath(rootPath, f)
                    .Split(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar)
                    .Any(part => excludedFolders.Contains(part)))
                .ToLookup(
                    f => Path.GetFileNameWithoutExtension(f),
                    StringComparer.OrdinalIgnoreCase);

            var csFiles = Directory.EnumerateFiles(
                rootPath,
                "*.cs",
                SearchOption.AllDirectories);

            var results = new List<AudioReference>();

            var pattern =
                @"Load<\s*(AudioEngine|WaveBank|Song|SoundEffect|SoundBank)\s*>\s*\(\s*[""']([^""']+)[""']";

            foreach (string csFile in csFiles)
            {
                string[] lines = File.ReadAllLines(csFile);

                for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
                {
                    string line = lines[lineNumber].Trim();

                    foreach (Match match in Regex.Matches(
                        line,
                        pattern,
                        RegexOptions.IgnoreCase))
                    {
                        string type = match.Groups[1].Value;
                        string reference = match.Groups[2].Value;

                        foreach (string file in contentFiles[
                            Path.GetFileName(reference)])
                        {
                            string fullPath = Path.GetFullPath(file);

                            if (fullPath.StartsWith(
                                outputPath + Path.DirectorySeparatorChar,
                                StringComparison.OrdinalIgnoreCase))
                                continue;

                            string extension = Path
                                .GetExtension(file)
                                .ToLowerInvariant();

                            bool isAudio = AudioExtensions.Contains(extension);
                            bool isContent = extension == ".xnb";

                            results.Add(new AudioReference(
                                line,
                                lineNumber + 1,
                                reference,
                                fullPath,
                                type,
                                isAudio,
                                isContent));

                        }
                    }
                }
            }

            results = results
                .OrderBy(x => x.LineNumber)
                .ThenBy(x => x.Reference)
                .ThenBy(x => x.File)
                .ToList();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Line")
                .AddColumn("C# Reference")
                .AddColumn("File")
                .AddColumn("Type")
                .AddColumn("Audio")
                .AddColumn("Content")
                .AddColumn("Status");

            table.AddRow(
                $"Found {results.Count} audio XACT references",
                "",
                "",
                "",
                "",
                "",
                "");

            var logPanels = new List<IRenderable>();

            AnsiConsole.Live(table).Start(ctx =>
            {
                foreach (AudioReference result in results)
                {
                    string name = Path.GetFileName(result.File);
                    string extension = Path
                        .GetExtension(result.File)
                        .ToLowerInvariant();

                    string audio = result.IsAudio
                        ? "[green]Yes[/]"
                        : "[grey]No[/]";

                    string content = result.IsContent
                        ? "[green]Yes[/]"
                        : "[grey]No[/]";

                    if (!VgmstreamExtensions.Contains(extension))
                    {
                        table.AddRow(
                            result.LineNumber.ToString(),
                            EscapeMarkup(result.Line),
                            name,
                            result.Type,
                            audio,
                            content,
                            "[yellow]Skipped[/]");

                        ctx.Refresh();
                        continue;
                    }

                    table.AddRow(
                        result.LineNumber.ToString(),
                        EscapeMarkup(result.Line),
                        name,
                        result.Type,
                        audio,
                        content,
                        "[yellow]Processing...[/]");

                    int row = table.Rows.Count - 1;

                    ctx.Refresh();

                    string log = RunVgmstream(result.File);

                    bool failed = log.StartsWith(
                        "ERROR:",
                        StringComparison.OrdinalIgnoreCase);

                    table.UpdateCell(
                        row,
                        6,
                        failed
                            ? "[red]Failed[/]"
                            : "[green]Complete[/]");

                    logPanels.Add(
                        new Panel(log)
                            .Header($"- vgmstream - {name} -")
                            .Border(BoxBorder.Rounded));

                    ctx.Refresh();
                }
            });

            AnsiConsole.WriteLine();

            foreach (IRenderable panel in logPanels)
            {
                AnsiConsole.Write(panel);
                AnsiConsole.WriteLine();
            }

            return results;
        }
        private static string RunVgmstream(string file)
        {
            string vgmstream = Path.Combine(
                AppContext.BaseDirectory,
                "tools",
                "vgmstream",
                "vgmstream-cli.exe");

            if (!File.Exists(vgmstream))
                return $"vgmstream not found: {vgmstream}";

            string output = Path.Combine(
                OutputRoot,
                Path.GetFileNameWithoutExtension(file) + ".wav");
            var psi = new ProcessStartInfo
            {
                FileName = vgmstream,
                Arguments = $"-o \"{output}\" \"{file}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);

            if (process == null)
                return "Failed to start vgmstream.";

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();

            process.WaitForExit();

            return stdout + stderr;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("XnbExtractor");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  XnbExtractor [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --input <path>          Input directory containing XNB files.");
            Console.WriteLine("  --output <path>         Output directory.");
            Console.WriteLine("  --loader                Run the XNB loader.");
            Console.WriteLine("  --parser                Run the XNB parser.");
            Console.WriteLine("  --extract               Extract XNB files.");
            Console.WriteLine("  --compress              Compress XNB files.");
            Console.WriteLine("  --convert-to-windows    Convert Xbox 360 XNB files to Windows format.");
            Console.WriteLine("  --overwrite             Overwrite existing output files.");
            Console.WriteLine("  --playaudio             Play audio files after extraction.");
            Console.WriteLine("  --help, -h              Display this help message.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine();
            Console.WriteLine("  XnbExtractor");
            Console.WriteLine("      Run using the default Debug paths.");
            Console.WriteLine();
            Console.WriteLine("  XnbExtractor --input <path> --output <path>");
            Console.WriteLine("      Specify input and output directories.");
            Console.WriteLine();
            Console.WriteLine("  XnbExtractor --input <path> --output <path> --extract");
            Console.WriteLine("      Extract XNB files.");
            Console.WriteLine();
            Console.WriteLine("  XnbExtractor --input <path> --output <path> --loader --parser");
            Console.WriteLine("      Run the loader and parser.");
            Console.WriteLine();
            Console.WriteLine("  XnbExtractor --input <path> --output <path> --extract --overwrite");
            Console.WriteLine("      Extract XNB files and overwrite existing files.");
        }

        private static bool ProcessOneOrMoreFiles(string input, bool loader, bool parser, List<AudioReference> audioReferences, ref int succeeded, ref int failed, int? limit)
        {
            if (Directory.Exists(input))
            {
                List<FileInfo> files = XnbCore.ListXnbFiles(input, limit: limit);

                if (files.Count == 0)
                {
                    Console.WriteLine("No XNB files found.");
                    return false;
                }

                Console.WriteLine($"Found {files.Count} XNB files.");
                List<DirectoryInfo> contentFolders = files
                    .Select(f => f.Directory!)
                    .DistinctBy(d => d.FullName)
                    .OrderBy(d => d.FullName)
                    .ToList();

                var tree = new Tree($"[yellow]Content Folders ({contentFolders.Count})[/]");

                foreach (DirectoryInfo folder in contentFolders)
                {
                    tree.AddNode($"[cyan]{Markup.Escape(folder.FullName)}[/]");
                }

                AnsiConsole.Write(tree);

                if (!loader && !parser)
                {
                    Console.WriteLine("No loader or parser specified. Use --loader and/or --parser.");
                    return false;
                }
                foreach (var file in files)
                {
                    try
                    {
                        XnbCore.ProcessFile(file.FullName, loader, parser);
                        succeeded++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {file.FullName}: {ex.Message}");
                        failed++;
                    }
                }
                Console.WriteLine($"Conversion complete. Failed: {failed} Succeeded: {succeeded}");

            }


            if (System.IO.File.Exists(input))
            {
                if (!input.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("FAILED| Input file is not an XNB.");
                    return true;
                }
                XnbCore.runLoaderTest = loader;
                XnbCore.ProcessFile(input, loader, parser);
                return true;
            }

            foreach (AudioReference audioReference in audioReferences)
            {
                if (System.IO.File.Exists(audioReference.File))
                {
                    XnbCore.ProcessFile(audioReference.File, loader, parser);
                }
                else
                {
                    Console.WriteLine($"FAILED| Audio reference file not found: {audioReference.File}");
                }
            }

            return true;
        }

        private static void LoadGameDependencies(string input)
        {
            var dll_files = XnbCore.ListDllFiles(input);

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
                        ReflectionReaderFactory.RegisterAssemblyReaders(asm);
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
        }

        //public static void RegisterAssemblyReaders(Assembly asm)
        //{
        //    foreach (Type type in asm.GetTypes())
        //    {
        //        Type? baseType = type.BaseType;

        //        if (baseType == null)
        //            continue;

        //        if (!baseType.IsGenericType)
        //            continue;

        //        if (baseType.GetGenericTypeDefinition().Name != "ContentTypeReader`1")
        //            continue;

        //        string name = type.FullName + "," + type.Assembly.GetName().Name;

        //        readers[name] = type;

        //        Console.WriteLine($"Registered {name}");
        //    }
        //}

        private static void RegisterReaders()
        {
            XnbTypeReaderFactory.Register("Microsoft.Xna.Framework.Content.EffectReader", new XnbEffectReader());
            XnbTypeReaderFactory.Register("Microsoft.Xna.Framework.Content.SongReader", new XnbSongReader());
            XnbTypeReaderFactory.Register("Microsoft.Xna.Framework.Content.Texture2DReader", new XnbTexture2DReader());
            XnbTypeReaderFactory.Register("Microsoft.Xna.Framework.Content.ModelReader", new XnbModelReader());
            XnbTypeReaderFactory.Register("Microsoft.Xna.Framework.Content.SpriteFontReader", new SpriteFontReader());
            XnbTypeReaderFactory.Register("Microsoft.Xna.Framework.Content.StringReader", new Content.Readers.StringReader());
            XnbTypeReaderFactory.Register("Microsoft.Xna.Framework.Content.Int32Reader", new XnbInt32Reader());
            XnbTypeReaderFactory.Register("Microsoft.Xna.Framework.Content.SoundEffectReader", new XnbSoundEffectReader());
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

        public static bool extract { get; private set; }
        public static string OutputRoot { get; set; }
        public static string InputRoot { get; set; }

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