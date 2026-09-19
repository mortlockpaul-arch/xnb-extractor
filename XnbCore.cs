using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using XnbExtractor.Content;
using XnbExtractor.Content.Readers;
using XnbExtractor.Content.Writers;
using XnbExtractor.Models;
using XnbExtractor.Readers;
using XnbExtractor.Xnb;
using static XnbExtractor.Utilities.TextureFormatDetector;
using File = System.IO.File;

namespace XnbExtractor
{
    internal static partial class Program
    {
        public static class XnbCore
        {
            private static void ConvertAudio(string inputFile, string outputFile)
            {
                string vgmstream = Path.Combine(
                    AppContext.BaseDirectory,
                    "tools",
                    "vgmstream",
                    "vgmstream-cli.exe");

                var psi = new ProcessStartInfo
                {
                    FileName = vgmstream,
                    Arguments = $"-o \"{outputFile}\" \"{inputFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi)!;

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();

                process.WaitForExit();

                Console.Write(stdout);

                if (!string.IsNullOrWhiteSpace(stderr))
                    Console.WriteLine(stderr);

                if (process.ExitCode != 0)
                    Console.WriteLine(
                        $"vgmstream failed with exit code {process.ExitCode}");
                else
                    Console.WriteLine($"Converted: {outputFile}");
            }
            public static void ProcessFile(string inputFile, bool runLoaderTest, bool runParserTest)
            {
                string relative = Utilities.GetRelativePath(InputRoot, inputFile);

                string outputFile = Path.Combine(OutputRoot, Path.ChangeExtension(relative, ".temp"));

                Console.WriteLine();
                Console.WriteLine(new string('=', 80));
                Console.WriteLine($"Input File: {inputFile}");
                Console.WriteLine($"Output File: {outputFile}");
                Console.WriteLine(new string('=', 80));

                if (runLoaderTest)
                    TestLoaderGame(inputFile);

                if (runParserTest)
                    ParseXnb(inputFile, outputFile);
            }

            public static List<FileInfo> ListDllFiles(string root, int? limit = null)
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

            public static List<FileInfo> ListXnbFiles(string root, int? limit = null)
            {
                var files = Directory.GetFiles(
                        root,
                        "*.xnb",
                        SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.Length);

                //if (!overwrite)
                //{
                //    if (Directory.Exists(output))
                //    {
                //        Directory.Delete(output, true);
                //        return;
                //    }
                //}
                //else { Console.WriteLine("Overwrite option is enabled."); }

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
                public object? Asset { get; init; }
                public TargetPlatform Platform { get; init; }
            }

            public static LoadedAsset LoadAsset(string inputFile)
            {
                using var xnb = new XnbFile(inputFile);

                var decompressed = xnb.Decompress();
                Console.WriteLine(BitConverter.ToString(decompressed, 0, 16));
                var content = new XnbContent();

                using var reader = new XnbContentReader(
                    new MemoryStream(decompressed),
                    content,
                    xnb.Header.Platform);

                XnbContentReader.ParseContent(reader);

                reader.Seek(content.DataPosition);

                return new LoadedAsset
                {
                    Asset = XnbTypeReaderFactory.ReadByType(
                        content.PrimaryReaderType,
                        reader),
                    Platform = xnb.Header.Platform
                };
            }

            public static void ExportAsset(LoadedAsset loaded, string inputFile, string outputFile)
            {
                Console.WriteLine($"ACTUAL ASSET TYPE: {loaded.Asset?.GetType().AssemblyQualifiedName}");

                if (loaded.Asset == null)
                {
                    ExportUnknown(null, outputFile);
                    return;
                }

                var type = loaded.Asset.GetType();
                var typeName = type.FullName;

                // Reflection-based isdf.Runtime types
                switch (typeName)
                {
                    case "isdf.Runtime.TBmp256":
                        ExportBmp256(loaded.Asset, inputFile, outputFile);
                        return;

                    case "isdf.Runtime.TEdgePal":
                        ExportPalette(loaded.Asset, outputFile);
                        return;

                    case "isdf.Runtime.TEventScriptSheet":
                        ExportEventScriptSheet(loaded.Asset, outputFile);
                        return;
                }

                // Reflection-based isdf.Runtime.TCSVtoArray[]
                if (loaded.Asset is Array array &&
                    array.GetType().GetElementType()?.FullName == "isdf.Runtime.TCSVtoArray")
                {
                    ExportCsvTables(array, outputFile);
                    return;
                }

                // Normal XnbExtractor types
                switch (loaded.Asset)
                {
                    case XnbEffect effect:
                        ExportEffect(effect, outputFile);
                        break;

                    case string text:
                        ExportStrings(text, outputFile);
                        break;

                    case string[] strings:
                        ExportStrings(strings, outputFile);
                        break;

                    case XnbTEventScriptSheet sheet:
                        ExportEventScriptSheet(sheet, outputFile);
                        break;

                    case XnbTBmp256 bmp:
                        ExportBmp256(bmp, inputFile, outputFile);
                        break;

                    case XnbTCSVtoArray[] tables:
                        ExportCsvTables(tables, outputFile);
                        break;

                    case CsvTable[] tables:
                        ExportCsvTables(tables, outputFile);
                        break;

                    case XnbModel model:
                        ExportModel(model);
                        break;

                    case XnbTexture texture:
                        ExportTexture(texture, outputFile);
                        break;

                    case SpriteFontData font:
                        Console.WriteLine($"SpriteFont glyphs: {font.Glyphs.Count}");
                        Console.WriteLine($"SpriteFont cropping: {font.Cropping.Count}");
                        Console.WriteLine($"SpriteFont characters: {font.Characters.Count}");
                        Console.WriteLine($"SpriteFont kerning: {font.Kerning.Count}");
                        ExportSpriteFont(font, outputFile);
                        break;

                    case XnbSoundEffect soundEffect:
                        Console.WriteLine("Exporting SoundEffect!");
                        Console.WriteLine($"Format: {soundEffect.Format}");
                        Console.WriteLine($"SampleRate: {soundEffect.SampleRate}");
                        Console.WriteLine($"Channels: {soundEffect.Channels}");
                        Console.WriteLine($"BitsPerSample: {soundEffect.BitsPerSample}");
                        Console.WriteLine($"ExtraData Length: {soundEffect.ExtraData?.Length ?? 0}");

                        string wavFile = Path.ChangeExtension(outputFile, ".wav");
                        Directory.CreateDirectory(Path.GetDirectoryName(wavFile)!);
                        ExportSoundEffect(soundEffect, wavFile);
                        break;

                    case XnbSong song:
                        ExportSong(song, inputFile, outputFile);
                        break;

                    default:
                        ExportUnknown(loaded.Asset, outputFile);
                        break;
                }
            }

            private static string GetAssetTypeName(object asset)
            {
                return asset.GetType().FullName ?? asset.GetType().Name;
            }

            private static string? FindReferencedAudio(string directory, string fileName)
            {
                // Exact filename first.
                string exact = Path.Combine(directory, fileName);

                if (File.Exists(exact))
                    return exact;

                // Try the same filename with common audio extensions.
                string withoutExtension =
                    Path.Combine(directory, Path.GetFileNameWithoutExtension(fileName));

                string[] extensions =
                {
                    ".wma",
                    ".wav",
                    ".ogg",
                    ".oga",
                    ".qoa"
                };

                foreach (string extension in extensions)
                {
                    string candidate = withoutExtension + extension;

                    if (File.Exists(candidate))
                        return candidate;
                }

                // Finally, search recursively. This is useful if the
                // referenced file isn't beside the XNB.
                string name = Path.GetFileName(fileName);

                string? recursive = Directory
                    .EnumerateFiles(
                        directory,
                        name,
                        SearchOption.AllDirectories)
                    .FirstOrDefault();

                return recursive;
            }
            private static void ExportSong(XnbSong song, string inputFile, string outputFile)
            {
                Console.WriteLine($"Song: {song.FileName}");
                Console.WriteLine($"Duration: {song.Duration} ms");

                // The SongReader stores the referenced audio filename,
                // e.g. "c2.wma".
                string songDirectory = Path.GetDirectoryName(inputFile)!;

                string? sourceFile = FindReferencedAudio(
                    songDirectory,
                    song.FileName);

                if (sourceFile == null)
                {
                    Console.WriteLine(
                        $"Could not find referenced audio: {song.FileName}");
                    return;
                }

                Console.WriteLine($"Referenced audio: {sourceFile}");

                // Preserve the original directory structure relative to Content.
                string relative = Utilities.GetRelativePath(
                    InputRoot,
                    sourceFile);

                // Convert the referenced audio to WAV.
                string wavOutput = Path.Combine(
                    OutputRoot,
                    Path.ChangeExtension(relative, ".wav"));

                Directory.CreateDirectory(
                    Path.GetDirectoryName(wavOutput)!);

                ConvertAudio(sourceFile, wavOutput);

                Console.WriteLine($"Exported song: {wavOutput}");
            }

            private static void ExportSoundEffect(XnbSoundEffect soundEffect, string outputFile)
            {
                Console.WriteLine($"Exporting sound effect to file: {outputFile} ({soundEffect.Format})");

                switch (soundEffect.Format)
                {
                    case AudioFormat.PCM:
                    case AudioFormat.ADPCM:
                        ExportWave(soundEffect, outputFile);
                        break;

                    case AudioFormat.XMA2:
                        ExportXma(soundEffect, outputFile);
                        break;

                    default:
                        ExportRaw(soundEffect, outputFile);
                        break;
                }
            }

            public static void PlayExportedAudio(string outputRoot)
            {
                string vgmstream = Path.Combine(
                    AppContext.BaseDirectory,
                    "tools",
                    "vgmstream",
                    "vgmstream-cli.exe");

                foreach (string file in Directory.EnumerateFiles(outputRoot, "*.wav", SearchOption.AllDirectories))
                {
                    // Skip files we've already decoded
                    if (file.EndsWith("_decoded.wav", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Console.WriteLine($"Playing {Path.GetFileName(file)}");

                    string decoded = Path.Combine(
                        Path.GetDirectoryName(file)!,
                        Path.GetFileNameWithoutExtension(file) + "_decoded.wav");

                    bool decodedOk = false;

                    // Try decoding with vgmstream first (XMA)
                    try
                    {
                        using var p = Process.Start(new ProcessStartInfo
                        {
                            FileName = vgmstream,
                            Arguments = $"-o \"{decoded}\" \"{file}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });

                        p?.WaitForExit();

                        decodedOk = File.Exists(decoded);
                        PlayWave(decodedOk ? decoded : file);
                    }
                    catch
                    {
                        // Not an XMA file (probably PCM/ADPCM)
                    }


                }
            }
            private static void ConvertPlay(string outputFile)
            {
                string xmaFile = Path.ChangeExtension(outputFile, ".wav");
                string decoded = Path.Combine(
                    Path.GetDirectoryName(xmaFile)!,
                    Path.GetFileNameWithoutExtension(xmaFile) + "_decoded.wav");
                string vgmstream = Path.Combine(
                    AppContext.BaseDirectory,
                    "tools",
                    "vgmstream",
                    "vgmstream-cli.exe");
                Process.Start(new ProcessStartInfo
                {
                    FileName = vgmstream,
                    Arguments = $"-o \"{decoded}\" \"{xmaFile}\"",
                    UseShellExecute = false
                })!.WaitForExit();

                PlayWave(decoded);
            }

            private static void PlayWave(string filename)
            {
                using var reader = new AudioFileReader(filename);
                using var player = new WaveOut();

                player.Init(reader);
                player.Play();

                while (player.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(100);
                }
            }

            private static void ExportRaw(XnbSoundEffect sound, string outputFile)
            {
                File.WriteAllBytes(
                    Path.ChangeExtension(outputFile, ".bin"),
                    sound.AudioData);
            }

            private static void ExportXma(XnbSoundEffect sound, string outputFile)
            {
                outputFile = Path.ChangeExtension(outputFile, ".wav");

                using var fs = File.Create(outputFile);
                using var writer = new BinaryWriter(fs);

                int fmtSize = 18 + sound.ExtraData.Length;

                int riffSize =
                    4 +
                    (8 + fmtSize) +
                    (8 + 4) +
                    (8 + sound.AudioData.Length);


                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(riffSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));


                // fmt chunk
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(fmtSize);

                writer.Write(sound.FormatTag);
                writer.Write(sound.Channels);
                writer.Write(sound.SampleRate);
                writer.Write(sound.AverageBytesPerSecond);
                writer.Write(sound.BlockAlign);
                writer.Write(sound.BitsPerSample);

                writer.Write((ushort)sound.ExtraData.Length);
                writer.Write(sound.ExtraData);


                // fact chunk
                writer.Write(Encoding.ASCII.GetBytes("fact"));
                writer.Write(4);

                // temporary
                writer.Write(GetSampleCount(sound.ExtraData));


                // data chunk
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(sound.AudioData.Length);
                writer.Write(sound.AudioData);


                Console.WriteLine($"Wrote {outputFile}");
            }

            private static uint GetSampleCount(byte[] extra)
            {
                return BitConverter.ToUInt32(extra, 6);
            }

            private static void ExportXmaWave(XnbSoundEffect sound, string outputFile)
            {
                Console.WriteLine("Exporting wave header for xma sound effect...");
                ExportWave(sound, outputFile);
            }
            private static void ExportEffect(XnbEffect effect, string outputFile)
            {
                string baseName = Path.Combine(
                    Path.GetDirectoryName(outputFile)!,
                    Path.GetFileNameWithoutExtension(outputFile));

                string fxbin = baseName + ".fxbin";
                string xnb = baseName + ".windows.xnb";
                var dir = Path.GetDirectoryName(outputFile);

                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllBytes(fxbin, effect.Data);

                Console.WriteLine($"Exported effect: {fxbin}");
                Console.WriteLine($"Effect data: {effect.Data.Length} bytes");

                XnbWriter.Write(
                    xnb,
                    new XnbContent(),
                    effect);

                Console.WriteLine($"Exported Windows XNB: {xnb}");
            }
            private static void ExportWave(XnbSoundEffect sound, string outputFile)
            {
                outputFile = Path.ChangeExtension(outputFile, ".wav");

                using var fs = File.Create(outputFile);
                using var writer = new BinaryWriter(fs);

                int fmtSize = 16 + (sound.ExtraData?.Length ?? 0);
                int riffSize = 4 +
                               (8 + fmtSize) +
                               (8 + sound.AudioData.Length);

                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(riffSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));

                // fmt chunk
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(fmtSize);

                writer.Write(sound.FormatTag);
                writer.Write(sound.Channels);
                writer.Write(sound.SampleRate);
                writer.Write(sound.AverageBytesPerSecond);
                writer.Write(sound.BlockAlign);
                writer.Write(sound.BitsPerSample);

                if (sound.ExtraData != null)
                {
                    writer.Write((ushort)sound.ExtraData.Length);
                    writer.Write(sound.ExtraData);
                }

                // data chunk
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(sound.AudioData.Length);
                writer.Write(sound.AudioData);

                Console.WriteLine($"fmt size: {fmtSize}");
                Console.WriteLine($"data size: {sound.AudioData.Length}");
                Console.WriteLine($"RIFF size: {riffSize}");
            }

            static void ExportSpriteFont(SpriteFontData font, string outputFile)
            {
                Console.WriteLine("=== SpriteFont Debug ===");
                Console.WriteLine($"Characters: {font.Characters.Count}");
                Console.WriteLine($"Glyphs: {font.Glyphs.Count}");
                Console.WriteLine($"Cropping: {font.Cropping.Count}");
                Console.WriteLine($"Kerning: {font.Kerning.Count}");
                Console.WriteLine($"LineSpacing: {font.LineSpacing}");
                Console.WriteLine($"Spacing: {font.Spacing}");
                Console.WriteLine($"DefaultCharacter: {font.DefaultCharacter}");
                Console.WriteLine( $"SpriteFont texture type: {font.Texture?.GetType().AssemblyQualifiedName}");
                string directory = Path.GetDirectoryName(outputFile)!;
                string name = Path.GetFileNameWithoutExtension(outputFile);

                string textureFile = Path.Combine(
                    directory,
                    name + "_texture.dds");

                string jsonFile = Path.Combine(
                    directory,
                    name + ".json");

                if (font.Texture is XnbTexture texture)
                {
                    Console.WriteLine(
                        $"Texture: {texture.Width}x{texture.Height}");

                    DdsWriter.Write(
                        texture,
                        textureFile);
                }
                else
                {
                    Console.WriteLine(
                        $"Unexpected SpriteFont texture type: " +
                        $"{font.Texture?.GetType().AssemblyQualifiedName}");
                }

                var metadata = new
                {
                    font.Spacing,
                    font.LineSpacing,
                    font.DefaultCharacter,
                    font.Characters,
                    font.Glyphs,
                    font.Cropping,
                    font.Kerning
                };

                File.WriteAllText(
                    jsonFile,
                    JsonSerializer.Serialize(
                        metadata,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        }));

                Console.WriteLine($"Exported SpriteFont: {name}");
            }

            private static void ExportStrings(string[] strings, string outputFile)
            {
                var textFile = outputFile + ".txt";

                Directory.CreateDirectory(
                    Path.GetDirectoryName(textFile)!);

                File.WriteAllLines(textFile, strings);
                Console.WriteLine("Exported strings to text file {0}", textFile);
            }

            private static void ExportStrings(string text, string outputFile)
            {
                var textFile = outputFile + ".txt";

                Directory.CreateDirectory(
                    Path.GetDirectoryName(textFile)!);

                File.WriteAllText(textFile, text);
                Console.WriteLine("Exported string to text file {0}", textFile);
            }

            private static void ExportEventScriptSheet(object sheet, string outputFile)
            {
                string textFile = Path.ChangeExtension(outputFile, ".txt");
                Directory.CreateDirectory(Path.GetDirectoryName(textFile)!);

                var sb = new StringBuilder();

                sb.AppendLine("=== TEventScriptSheet ===");
                sb.AppendLine();

                var lines = GetMemberValue(sheet, "Lines") as Array;

                sb.AppendLine($"Lines: {lines?.Length ?? 0}");
                sb.AppendLine();

                if (lines != null)
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        object line = lines.GetValue(i)!;

                        sb.AppendLine($"--- Line {i} ---");

                        AppendMember(sb, line, "ScrType");
                        AppendMember(sb, line, "ScrVar0");
                        AppendMember(sb, line, "ScrVar1");
                        AppendMember(sb, line, "ScrVar2");
                        AppendMember(sb, line, "ScrVar3");

                        sb.AppendLine($"LoadSentence: {GetMemberValue(line, "LoadSentence")}");
                        sb.AppendLine($"MoveSentence: {GetMemberValue(line, "MoveSentence")}");
                        sb.AppendLine();
                    }
                }

                File.WriteAllText(textFile, sb.ToString());
                Console.WriteLine($"Exported TEventScriptSheet: {textFile}");
            }

            private static void ExportBmp256(object bmp, string inputFile, string outputFile)
            {
                int width = (int)GetMemberValue(bmp, "Width")!;
                int height = (int)GetMemberValue(bmp, "Height")!;
                byte[] palIdx = (byte[])GetMemberValue(bmp, "PalIdx")!;

                Console.WriteLine($"TBmp256: {width}x{height}");
                Console.WriteLine($"PalIdx: {palIdx.Length} bytes");

                DirectoryInfo? dir = new FileInfo(inputFile).Directory;

                while (dir != null && dir.Name != "Content")
                    dir = dir.Parent;

                if (dir == null)
                    throw new DirectoryNotFoundException(
                        $"Could not find Content directory for: {inputFile}");

                string contentRoot = dir.FullName;
                var palettes = new List<XnbTEdgePal>();

                for (int i = 0; ; i++)
                {
                    string palFile = Path.Combine(
                        contentRoot,
                        "data",
                        $"pal{i}.xnb");

                    if (!File.Exists(palFile))
                        break;

                    var loadedPalette = LoadAsset(palFile);
                    var paletteAsset = loadedPalette.Asset;

                    Console.WriteLine(
                        $"Palette {i}: {paletteAsset?.GetType().FullName}");

                    if (paletteAsset == null)
                        throw new InvalidDataException(
                            $"Palette {i} loaded as null: {palFile}");

                    uint[] palARGB = (uint[])GetMemberValue(
                        paletteAsset,
                        "PalARGB")!;

                    Console.WriteLine(
                        $"Palette {i}: {palARGB.Length} colours");

                    palettes.Add(new XnbTEdgePal(palARGB));
                }
                Console.WriteLine($"Loaded {palettes.Count} palettes");
                if (palettes.Count == 0)
                    throw new InvalidDataException(
                        $"No palette XNBs found in {Path.Combine(contentRoot, "data")}");

                var manager = new XnbTEdgePalManager(palettes.ToArray());

                uint[] paletteData = manager.GetPalAll(
                    0, 0, 0, 0, 0, 0, 0, 0, 0);

                Console.WriteLine($"Extracted TBmp256 {width}x{height}");
                Console.WriteLine($"Palette colours: {paletteData.Length}");

                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

                File.WriteAllBytes(
                    Path.ChangeExtension(outputFile, ".palidx"),
                    palIdx);

                TBmp256PngWriter.Write(
                    width,
                    height,
                    palIdx,
                    paletteData,
                    Path.ChangeExtension(outputFile, ".png"));

                Console.WriteLine($"Exported TBmp256 to PNG and palette index file. " + $"{Path.ChangeExtension(outputFile, ".png")}");
            }


            private static void ExportCsvTables(Array tables, string outputFile)
            {
                var sb = new StringBuilder();

                for (int i = 0; i < tables.Length; i++)
                {
                    var table = tables.GetValue(i)!;

                    var strings = (string[])GetMemberValue(table, "stringArray")!;

                    var values = (int[])GetMemberValue(table, "IntArray")!;

                    sb.AppendLine($"=== Table {i} ===");

                    int count = Math.Min(strings.Length, values.Length);

                    for (int j = 0; j < count; j++)
                        sb.AppendLine($"{strings[j]} = {values[j]}");

                    sb.AppendLine();
                }
                Console.WriteLine("Exported CSV tables to text file {0}", Path.ChangeExtension(outputFile, ".txt"));
                File.WriteAllText(Path.ChangeExtension(outputFile, ".txt"), sb.ToString());
            }

            private static void ExportCsvTables(CsvTable[] tables, string outputFile)
            {
                string csvFile = Path.ChangeExtension(outputFile, ".csv");

                using var writer = new StreamWriter(csvFile, false, Encoding.UTF8);

                foreach (var table in tables)
                {
                    int count = Math.Max(table.Strings.Length, table.Indices.Length);

                    for (int i = 0; i < count; i++)
                    {
                        string text = i < table.Strings.Length ? table.Strings[i] : "";
                        string value = i < table.Indices.Length ? table.Indices[i].ToString() : "";

                        writer.WriteLine($"{text},{value}");
                    }

                    writer.WriteLine();
                }

                Console.WriteLine($"Exported CSV tables: {csvFile}");
            }

            private static void ExportModel(XnbModel model)
            {
                Console.WriteLine("Model loaded!");
                Console.WriteLine($"Meshes: {model.Meshes.Count}");
            }

            private static void ExportPalette(object palette, string outputFile)
            {
                uint[] palARGB = (uint[])GetMemberValue(palette, "PalARGB")!;

                Console.WriteLine("Palette Loaded");
                Console.WriteLine($"Colours: {palARGB.Length}");

                using var image = new Bitmap(256, 256);
                using var g = Graphics.FromImage(image);

                for (int i = 0; i < palARGB.Length; i++)
                {
                    int x = (i % 16) * 16;
                    int y = (i / 16) * 16;
                    uint color = palARGB[i];

                    using var brush = new SolidBrush(
                        Color.FromArgb(
                            (byte)(color >> 24),
                            (byte)(color >> 16),
                            (byte)(color >> 8),
                            (byte)color));

                    g.FillRectangle(brush, x, y, 16, 16);
                }

                string pngFile = Path.ChangeExtension(outputFile, ".png");

                Directory.CreateDirectory(Path.GetDirectoryName(pngFile)!);

                image.Save(
                    pngFile,
                    System.Drawing.Imaging.ImageFormat.Png);

                Console.WriteLine($"Exported palette: {pngFile}");
            }

            private static void ExportTexture(XnbTexture texture, string outputFile)
            {
                Console.WriteLine("Texture loaded!");
                Console.WriteLine($"{texture.Width}x{texture.Height}");
                Console.WriteLine($"MipCount: {texture.MipCount}");
                Console.WriteLine($"Raw Format: {texture.RawFormat}");
                Console.WriteLine($"Raw bytes: {texture.MipData[0].Length}");
                string pngFile = Path.ChangeExtension(outputFile, ".png");
                switch (texture.Format)
                {
                    case TextureFormat.Dxt1:
                    case TextureFormat.Dxt3:
                    case TextureFormat.Dxt5:
                        ExportDds(texture, outputFile);
                        break;
                    case TextureFormat.Color:
                        Directory.CreateDirectory(Path.GetDirectoryName(pngFile)!);
                        ExportTexturePng(texture, pngFile);
                        break;
                    default:

                        Directory.CreateDirectory(Path.GetDirectoryName(pngFile)!);
                        ExportTexturePng(texture, pngFile);
                        break;
                }
            }


            private static void ExportTexturePng(XnbTexture texture, string outputFile)
            {
                if (texture.MipData.Count == 0)
                    throw new InvalidOperationException(
                        "Texture has no mip data.");

                byte[] source = texture.MipData[0];

                byte[] rgba = new byte[source.Length];

                for (int i = 0; i < source.Length; i += 4)
                {
                    rgba[i + 0] = source[i + 3]; // R
                    rgba[i + 1] = source[i + 2]; // G
                    rgba[i + 2] = source[i + 1]; // B
                    rgba[i + 3] = source[i + 0]; // A
                }

                PngWriter.Write(
                    outputFile,
                    texture.Width,
                    texture.Height,
                    rgba);

                Console.WriteLine($"Exported PNG: {outputFile}");
            }

            private static void ExportDds(XnbTexture texture, string ddsFile)
            {
                DdsWriter.Write(texture, ddsFile);
            }

            private static void ExportUnknown(object asset, string outputFile)
            {
                var dir = Path.GetDirectoryName(outputFile);

                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

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

                    Console.WriteLine($"Asset: {asset.Asset.GetType().Name}");
                    Console.WriteLine($"Platform: {asset.Platform}");

                    ExportAsset(asset, inputFile, outputFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }


            private static object? GetMemberValue(object obj, string name)
            {
                var type = obj.GetType();

                var property = type.GetProperty(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.IgnoreCase);

                if (property != null)
                    return property.GetValue(obj);

                var field = type.GetField(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.IgnoreCase);

                return field?.GetValue(obj);
            }

            private static void AppendMember(
                StringBuilder sb,
                object? obj,
                string name)
            {
                if (obj == null)
                    return;

                var value = GetMemberValue(obj, name);

                if (value == null)
                {
                    sb.AppendLine($"{name}: null");
                    return;
                }

                if (value is Array array)
                {
                    sb.AppendLine(
                        $"{name}: [{string.Join(", ", array.Cast<object>())}]");

                    return;
                }

                sb.AppendLine($"{name}: {value}");
            }
        }
    }
}