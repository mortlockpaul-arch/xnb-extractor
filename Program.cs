using Monogame_Compression.Content.Readers;
using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using XnbExtractor.Content.Readers;
using XnbExtractor.Readers;
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
                }
            }

            if (args.Contains("--help") || args.Contains("-h"))
            {
                PrintHelp();
#if !DEBUG
                    Console.WriteLine();
                    Console.WriteLine("Press Enter to exit...");
                    Console.ReadLine();
#endif
                return;
            }

#if DEBUG
            if (string.IsNullOrWhiteSpace(input))
            {
                input = @"C:\source\Indie-Games\indie-game-archive\invAIders (World) (XBLIG)\";
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                output = @"C:\source\Indie-Games\indie-game-archive\invAIders (World) (XBLIG)\Output";
            }
#else
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
                {
                    PrintHelp();

                    Console.WriteLine();
                    Console.WriteLine("Press Enter to exit...");
                    Console.ReadLine();
                    return;
                }
#endif

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

            AnsiConsole.Write(table);

            RegisterReaders();

            int succeeded = 0;
            int failed = 0;

            if (scanContent)
            {
                ScanContentForAudio(InputRoot);
            }
            if (scanCs)
            {
                ScanCsForAudio(InputRoot);
            }
            if(scanContent || scanCs)
            {
                Console.WriteLine("Audio scan complete. Press any key to exit.");
                Console.ReadKey();
                return;
            }
            bool flowControl = ProcessOneOrMoreFiles(input, loader, parser);
            if (flowControl)
            {
                return;
            }

            flowControl = ProcessFolder(input, loader, parser, playaudio, ref succeeded, ref failed);
            if (flowControl)
            {
                return;
            }
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

        private static void ScanCsForAudio(string root)
        {
            string rootPath = Path.GetFullPath(root);
            string outputPath = Path.GetFullPath(OutputRoot);

            var csFiles = Directory.EnumerateFiles(
                rootPath,
                "*.cs",
                SearchOption.AllDirectories);

            var audioFiles = new Dictionary<string, HashSet<string>>(
                StringComparer.OrdinalIgnoreCase);

            foreach (string csFile in csFiles)
            {
                string source = File.ReadAllText(csFile);
                string csDirectory = Path.GetDirectoryName(csFile)!;

                foreach (Match match in Regex.Matches(
                    source,
                    @"(?:new\s+)?(AudioEngine|WaveBank|SoundBank)\s*\([^)]*?[""']([^""']+\.[a-z0-9]+)[""']",
                    RegexOptions.IgnoreCase))
                {
                    string type = match.Groups[1].Value;
                    string reference = match.Groups[2].Value;

                    string extension = Path.GetExtension(reference)
                        .ToLowerInvariant();

                    if (!AudioExtensions.Contains(extension))
                        continue;

                    string? path = null;

                    // Try relative to the .cs file.
                    string candidate = Path.GetFullPath(
                        Path.Combine(csDirectory, reference));

                    if (File.Exists(candidate))
                    {
                        path = candidate;
                    }
                    else
                    {
                        // Try relative to the game root.
                        candidate = Path.GetFullPath(
                            Path.Combine(rootPath, reference));

                        if (File.Exists(candidate))
                            path = candidate;
                    }

                    // Search the whole game tree if it wasn't found.
                    if (path == null)
                    {
                        string relativeReference = reference
                            .Replace('/', Path.DirectorySeparatorChar)
                            .Replace('\\', Path.DirectorySeparatorChar);

                        string fileName = Path.GetFileName(reference);

                        foreach (string found in Directory.EnumerateFiles(
                            rootPath,
                            fileName,
                            SearchOption.AllDirectories))
                        {
                            string fullPath = Path.GetFullPath(found);

                            if (fullPath.StartsWith(
                                outputPath + Path.DirectorySeparatorChar,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            if (fullPath.EndsWith(
                                Path.DirectorySeparatorChar + relativeReference,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                path = fullPath;
                                break;
                            }
                        }
                    }

                    if (path == null)
                        continue;

                    if (!audioFiles.TryGetValue(path, out HashSet<string>? types))
                    {
                        types = [];
                        audioFiles[path] = types;
                    }

                    types.Add(type);
                }
            }

            var files = audioFiles
                .Where(x =>
                {
                    string fullPath = Path.GetFullPath(x.Key);

                    return !fullPath.StartsWith(
                        outputPath + Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(x => x.Key)
                .ToList();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("File")
                .AddColumn("Type")
                .AddColumn("Status");

            table.AddRow(
                $"Found {files.Count} audio files xact references",
                "",
                "");

            var logPanels = new List<IRenderable>();

            AnsiConsole.Live(table).Start(ctx =>
            {
                foreach (var entry in files)
                {
                    string file = entry.Key;
                    string name = Path.GetFileName(file);
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    string type = string.Join(", ", entry.Value.OrderBy(x => x));

                    if (!VgmstreamExtensions.Contains(ext))
                    {
                        table.AddRow(
                            name,
                            type,
                            "[yellow]Skipped[/]");

                        ctx.Refresh();
                        continue;
                    }

                    table.AddRow(
                        name,
                        type,
                        "[yellow]Processing...[/]");

                    int row = table.Rows.Count - 1;

                    ctx.Refresh();

                    string log = RunVgmstream(file);

                    bool failed = log.StartsWith(
                        "ERROR:",
                        StringComparison.OrdinalIgnoreCase);

                    table.UpdateCell(
                        row,
                        2,
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

        private static bool ProcessFolder(string input, bool loader, bool parser, bool playaudio, ref int succeeded, ref int failed)
        {
            if (Directory.Exists(input))
            {
                LoadGameDependencies(input);
                var files = XnbCore.ListXnbFiles(input);

                if (files.Count == 0)
                {
                    Console.WriteLine("FAILED| No XNB files found.");
                    return false;
                }

                Console.WriteLine($"Found {files.Count} XNB files.");

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
            else { Console.WriteLine($"FAILED| Path not found: {input}"); }

            if (playaudio) { XnbCore.PlayExportedAudio(OutputRoot); }
            Environment.Exit(failed == 0 ? 0 : 1);
            return true;
        }

        private static bool ProcessOneOrMoreFiles(string input, bool loader, bool parser)
        {
            if (System.IO.File.Exists(input))
            {
                if (!input.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("FAILED| Input file is not an XNB.");
                    return true;
                }

                XnbCore.ProcessFile(input, loader, parser);
                return true;
            }

            return false;
        }

        private static void LoadGameDependencies(string input)
        {
            var dll_files = XnbCore.ListDllFiles(Directory.GetParent(input)!.FullName);

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