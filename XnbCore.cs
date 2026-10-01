using Microsoft.Xna.Framework.Audio;
using NAudio.Wave;
using Spectre.Console;
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
                    AnsiConsole.MarkupLine(stderr);

                if (process.ExitCode != 0)
                    AnsiConsole.MarkupLine(
                        $"vgmstream failed with exit code {process.ExitCode}");
                else
                    AnsiConsole.MarkupLine($"Converted: {outputFile}");
            }
            public static void ProcessFile(string inputFile, bool runLoaderTest, bool runParserTest)
            {
                if (!string.Equals(Path.GetExtension(inputFile), ".xnb", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string relative = Utilities.GetRelativePath(InputRoot, inputFile);

                string outputFile = Path.Combine(OutputRoot, Path.ChangeExtension(relative, ".temp"));

                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn("Property")
                    .AddColumn("Value");

                table.AddRow("Input", Markup.Escape(inputFile));
                table.AddRow("Output", Markup.Escape(outputFile));

                AnsiConsole.Write(table);

                if (runParserTest)
                    ParseXnb(inputFile, outputFile, runLoaderTest);
            }

            private static readonly HashSet<string> excludedFolders =
                new(["bin", "obj", "Output"], StringComparer.OrdinalIgnoreCase);

            public static bool runLoaderTest { get; set; }

            private static IEnumerable<string> EnumerateFilesExcluding(
                string root,
                string pattern)
            {
                foreach (string file in Directory.EnumerateFiles(root, pattern))
                    yield return file;

                foreach (string directory in Directory.EnumerateDirectories(root))
                {
                    if (excludedFolders.Contains(Path.GetFileName(directory)))
                        continue;

                    foreach (string file in EnumerateFilesExcluding(directory, pattern))
                        yield return file;
                }
            }

            public static List<FileInfo> ListDllFiles(string root, int? limit = null)
            {
                IEnumerable<FileInfo> files = EnumerateFilesExcluding(root, "*.dll")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.Length);

                if (limit.HasValue)
                    files = files.Take(limit.Value);

                var list = files.ToList();

                int index = 1;

                foreach (var file in list)
                {
                    var relative = Utilities.GetRelativePath(root, file.FullName);
                    double sizeMb = file.Length / 1024d / 1024d;

                    AnsiConsole.MarkupLine(
                        $"{index++,4} | {file.Name,-40} {sizeMb,8:F2} MB | {relative}");
                }

                return list;
            }
            public static List<FileInfo> ListXnbFiles(string root, int? limit = null)
            {
                IEnumerable<FileInfo> files = Directory.GetFiles(
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
                //else { AnsiConsole.MarkupLine("Overwrite option is enabled."); }

                if (limit.HasValue)
                {
                    AnsiConsole.MarkupLine($"Limiting to {limit.Value} files.");
                    files = files.Take(limit.Value);
                }

                var list = files.ToList();

                int index = 1;

                foreach (var file in list)
                {
                    var relative = Utilities.GetRelativePath(root, file.FullName);
                    double sizeMb = file.Length / 1024d / 1024d;

                    AnsiConsole.MarkupLine(
                        $"{index++,4} | {file.Name,-40} {sizeMb,8:F2} MB | {relative}");
                }

                return list;
            }

            public class LoadedAsset
            {
                public LoadedAsset()
                {
                }

                public LoadedAsset(XnbEffect effect)
                {
                    Asset = effect;
                }

                public object? Asset { get; init; }
                public TargetPlatform Platform { get; init; }
            }

            public static LoadedAsset LoadAsset(string inputFile)
            {
                using var xnb = new XnbFile(inputFile);

                var decompressed = xnb.Decompress();
                AnsiConsole.MarkupLine(BitConverter.ToString(decompressed, 0, 16));
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

            public static void ExportAsset(LoadedAsset loaded, string inputFile, string outputFile, bool runLoaderTest)
            {
                AnsiConsole.MarkupLine($"ACTUAL ASSET TYPE: {loaded.Asset?.GetType().AssemblyQualifiedName}");

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
                        AnsiConsole.MarkupLine($"SpriteFont glyphs: {font.Glyphs.Count}");
                        AnsiConsole.MarkupLine($"SpriteFont cropping: {font.Cropping.Count}");
                        AnsiConsole.MarkupLine($"SpriteFont characters: {font.Characters.Count}");
                        AnsiConsole.MarkupLine($"SpriteFont kerning: {font.Kerning.Count}");
                        ExportSpriteFont(font, outputFile);
                        break;

                    case XnbSoundEffect soundEffect:
                        AnsiConsole.MarkupLine("Exporting SoundEffect!");
                        AnsiConsole.MarkupLine($"Format: {soundEffect.Format}");
                        AnsiConsole.MarkupLine($"SampleRate: {soundEffect.SampleRate}");
                        AnsiConsole.MarkupLine($"Channels: {soundEffect.Channels}");
                        AnsiConsole.MarkupLine($"BitsPerSample: {soundEffect.BitsPerSample}");
                        AnsiConsole.MarkupLine($"ExtraData Length: {soundEffect.ExtraData?.Length ?? 0}");

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
                XnbWriter.Write(outputFile, new XnbContent(), loaded, runLoaderTest);


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
                AnsiConsole.MarkupLine($"Song: {song.FileName}");
                AnsiConsole.MarkupLine($"Duration: {song.Duration} ms");

                // The SongReader stores the referenced audio filename,
                // e.g. "c2.wma".
                string songDirectory = Path.GetDirectoryName(inputFile)!;

                string? sourceFile = FindReferencedAudio(
                    songDirectory,
                    song.FileName);

                if (sourceFile == null)
                {
                    AnsiConsole.MarkupLine(
                        $"Could not find referenced audio: {song.FileName}");
                    return;
                }

                AnsiConsole.MarkupLine($"Referenced audio: {sourceFile}");

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

                AnsiConsole.MarkupLine($"Exported song: {wavOutput}");
            }

            private static void ExportSoundEffect(XnbSoundEffect soundEffect, string outputFile)
            {
                AnsiConsole.MarkupLine($"Exporting sound effect to file: {outputFile} ({soundEffect.Format})");

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

                string baseName = Path.Combine(
     Path.GetDirectoryName(outputFile)!,
     Path.GetFileNameWithoutExtension(outputFile));

                string xnb = baseName + ".xnb";
                var dir = Path.GetDirectoryName(outputFile);


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

                    AnsiConsole.MarkupLine($"Playing {Path.GetFileName(file)}");

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


                AnsiConsole.MarkupLine($"Wrote {outputFile}");
            }

            private static uint GetSampleCount(byte[] extra)
            {
                return BitConverter.ToUInt32(extra, 6);
            }

            private static void ExportXmaWave(XnbSoundEffect sound, string outputFile)
            {
                AnsiConsole.MarkupLine("Exporting wave header for xma sound effect...");
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

                AnsiConsole.MarkupLine($"Exported effect: {fxbin}");
                AnsiConsole.MarkupLine($"Effect data: {effect.Data.Length} bytes");

                XnbWriter.Write(
                    xnb,
                    new XnbContent(),
                    new LoadedAsset(effect),
                    false);

                AnsiConsole.MarkupLine($"Exported Windows XNB: {xnb}");
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

                AnsiConsole.MarkupLine($"fmt size: {fmtSize}");
                AnsiConsole.MarkupLine($"data size: {sound.AudioData.Length}");
                AnsiConsole.MarkupLine($"RIFF size: {riffSize}");
            }

            static void ExportSpriteFont(SpriteFontData font, string outputFile)
            {
                AnsiConsole.MarkupLine("=== SpriteFont Debug ===");
                AnsiConsole.MarkupLine($"Characters: {font.Characters.Count}");
                AnsiConsole.MarkupLine($"Glyphs: {font.Glyphs.Count}");
                AnsiConsole.MarkupLine($"Cropping: {font.Cropping.Count}");
                AnsiConsole.MarkupLine($"Kerning: {font.Kerning.Count}");
                AnsiConsole.MarkupLine($"LineSpacing: {font.LineSpacing}");
                AnsiConsole.MarkupLine($"Spacing: {font.Spacing}");
                AnsiConsole.MarkupLine($"DefaultCharacter: {font.DefaultCharacter}");
                AnsiConsole.MarkupLine($"SpriteFont texture type: {font.Texture?.GetType().AssemblyQualifiedName}");
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
                    AnsiConsole.MarkupLine(
                        $"Texture: {texture.Width}x{texture.Height}");

                    DdsWriter.Write(
                        texture,
                        textureFile);
                }
                else
                {
                    AnsiConsole.MarkupLine(
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

                AnsiConsole.MarkupLine($"Exported SpriteFont: {name}");
            }

            private static void ExportStrings(string[] strings, string outputFile)
            {
                var textFile = outputFile + ".txt";

                Directory.CreateDirectory(
                    Path.GetDirectoryName(textFile)!);

                File.WriteAllLines(textFile, strings);
                AnsiConsole.MarkupLine("Exported strings to text file {0}", textFile);
            }

            private static void ExportStrings(string text, string outputFile)
            {
                var textFile = outputFile + ".txt";

                Directory.CreateDirectory(
                    Path.GetDirectoryName(textFile)!);

                File.WriteAllText(textFile, text);
                AnsiConsole.MarkupLine("Exported string to text file {0}", textFile);
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
                AnsiConsole.MarkupLine($"Exported TEventScriptSheet: {textFile}");
            }

            private static void ExportBmp256(object bmp, string inputFile, string outputFile)
            {
                int width = (int)GetMemberValue(bmp, "Width")!;
                int height = (int)GetMemberValue(bmp, "Height")!;
                byte[] palIdx = (byte[])GetMemberValue(bmp, "PalIdx")!;

                AnsiConsole.MarkupLine($"TBmp256: {width}x{height}");
                AnsiConsole.MarkupLine($"PalIdx: {palIdx.Length} bytes");

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

                    AnsiConsole.MarkupLine(
                        $"Palette {i}: {paletteAsset?.GetType().FullName}");

                    if (paletteAsset == null)
                        throw new InvalidDataException(
                            $"Palette {i} loaded as null: {palFile}");

                    uint[] palARGB = (uint[])GetMemberValue(
                        paletteAsset,
                        "PalARGB")!;

                    AnsiConsole.MarkupLine(
                        $"Palette {i}: {palARGB.Length} colours");

                    palettes.Add(new XnbTEdgePal(palARGB));
                }
                AnsiConsole.MarkupLine($"Loaded {palettes.Count} palettes");
                if (palettes.Count == 0)
                    throw new InvalidDataException(
                        $"No palette XNBs found in {Path.Combine(contentRoot, "data")}");

                var manager = new XnbTEdgePalManager(palettes.ToArray());

                uint[] paletteData = manager.GetPalAll(
                    0, 0, 0, 0, 0, 0, 0, 0, 0);

                AnsiConsole.MarkupLine($"Extracted TBmp256 {width}x{height}");
                AnsiConsole.MarkupLine($"Palette colours: {paletteData.Length}");

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

                AnsiConsole.MarkupLine($"Exported TBmp256 to PNG and palette index file. " + $"{Path.ChangeExtension(outputFile, ".png")}");
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
                AnsiConsole.MarkupLine("Exported CSV tables to text file {0}", Path.ChangeExtension(outputFile, ".txt"));
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

                AnsiConsole.MarkupLine($"Exported CSV tables: {csvFile}");
            }

            private static void ExportModel(XnbModel model)
            {
                AnsiConsole.MarkupLine("Model loaded!");
                AnsiConsole.MarkupLine($"Meshes: {model.Meshes.Count}");
            }

            private static void ExportPalette(object palette, string outputFile)
            {
                uint[] palARGB = (uint[])GetMemberValue(palette, "PalARGB")!;

                AnsiConsole.MarkupLine("Palette Loaded");
                AnsiConsole.MarkupLine($"Colours: {palARGB.Length}");

                using var image = new Bitmap(256, 256);
                using var g = Graphics.FromImage(image);

                for (int i = 0; i < palARGB.Length; i++)
                {
                    int x = (i % 16) * 16;
                    int y = (i / 16) * 16;
                    uint color = palARGB[i];

                    using var brush = new SolidBrush(
                        System.Drawing.Color.FromArgb(
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

                AnsiConsole.MarkupLine($"Exported palette: {pngFile}");
            }

            private static void ExportTexture(XnbTexture texture, string outputFile)
            {
                AnsiConsole.MarkupLine("Texture loaded!");
                AnsiConsole.MarkupLine($"{texture.Width}x{texture.Height}");
                AnsiConsole.MarkupLine($"MipCount: {texture.MipCount}");
                AnsiConsole.MarkupLine($"Raw Format: {texture.RawFormat}");
                AnsiConsole.MarkupLine($"Raw bytes: {texture.MipData[0].Length}");
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

                AnsiConsole.MarkupLine($"Exported PNG: {outputFile}");
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

                AnsiConsole.MarkupLine(
                    $"Exported unknown asset {asset.GetType()}");
            }

            static void ParseXnb(string inputFile, string outputFile, bool runLoaderTest)
            {
                try
                {
                    LoadedAsset asset = LoadAsset(inputFile);
                    if (asset.Asset == null)
                    {
                        AnsiConsole.MarkupLine($"Loaded asset is null for file: {inputFile}");
                        return;
                    }
                    string type = asset.Asset.GetType().FullName ?? asset.Asset.GetType().Name;

                    AnsiConsole.MarkupLine($"Loaded asset type: {type}");
                    AnsiConsole.MarkupLine($"Asset: {asset.Asset.GetType().Name}");
                    AnsiConsole.MarkupLine($"Platform: {asset.Platform}");

                    ExportAsset(asset, inputFile, outputFile, runLoaderTest);

                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine(ex.Message);
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