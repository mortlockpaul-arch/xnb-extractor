using Monogame_Compression.Content.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using XnbExtractor.Content.Readers;
using XnbExtractor.Readers;
using File = System.IO.File;

namespace XnbExtractor
{
    internal static partial class Program
    {
        static string inputRoot;
        static string outputRoot;

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

            Console.WriteLine("XNB Extractor");

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
                    case "--input": input = args[++i]; break;
                    case "--output": output = args[++i]; break;
                    case "--loader": loader = true; break;
                    case "--parser": parser = true; break;
                    case "--extract": extract = true; break;
                    case "--compress": compressXnb = true; break;
                    case "--convert-to-windows": convert_to_windows = true; break;
                    case "--overwrite": overwrite = true; break;
                    case "--playaudio": playaudio = true; break;
                }
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                input = @"C:\PycharmProjects\xenia-game-manager\src\downloads\XBLIG\Downtown SMASH Dodgeball! (World) (XBLIG)\584E07D2\00000002\extracted\584E07D1\";
            }
            if (string.IsNullOrWhiteSpace(output))
            {
                output = @"C:\PycharmProjects\xenia-game-manager\src\downloads\XBLIG\Downtown SMASH Dodgeball! (World) (XBLIG)\584E07D2\00000002\extracted\584E07D1\";
            }

            Console.WriteLine($"Input      : {input}");
            Console.WriteLine($"Output     : {output}");
            Console.WriteLine($"Loader     : {loader}");
            Console.WriteLine($"Parser     : {parser}");
            Console.WriteLine($"Compress XNB : {compressXnb}");
            Console.WriteLine($"Convert to Windows : {convert_to_windows}");
            Console.WriteLine($"Overwrite  : {overwrite}");
            Console.WriteLine($"Play Audio : {playaudio}");

            RegisterReaders();

            if (!Directory.Exists(output)) Directory.CreateDirectory(output);
            inputRoot = Directory.Exists(input) ? input : Path.GetDirectoryName(input) ?? input;
            outputRoot = Directory.Exists(output) ? output : Path.GetDirectoryName(output) ?? output;

            if ( !Directory.Exists(inputRoot) ) Directory.CreateDirectory(inputRoot);
           

            Console.WriteLine($"Input Root  : {inputRoot}");
            Console.WriteLine($"Output Root : {outputRoot}");

            int succeeded = 0;
            int failed = 0;

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

            if (playaudio) { XnbCore.PlayExportedAudio(outputRoot); }
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

            XnbTypeReaderFactory.Register(
                "Microsoft.Xna.Framework.Content.ModelReader",
                new XnbModelReader());

            XnbTypeReaderFactory.Register(
                "Microsoft.Xna.Framework.Content.SpriteFontReader",
                new SpriteFontReader());

            XnbTypeReaderFactory.Register(
                "Microsoft.Xna.Framework.Content.StringReader",
                new Content.Readers.StringReader());

            XnbTypeReaderFactory.Register(
                "Microsoft.Xna.Framework.Content.Int32Reader",
                new XnbInt32Reader());
            XnbTypeReaderFactory.Register(
                "Microsoft.Xna.Framework.Content.SoundEffectReader",
                new XnbSoundEffectReader()
            );
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