
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace XnbExtractor;

public static class XnaVersionDetector
{
    public sealed record XnaDetection(
        string FileName,
        string XnaVersion,
        string AssemblyName,
        Version AssemblyVersion
    );

    public static List<XnaDetection> Detect(string root)
    {
        var results = new List<XnaDetection>();

        foreach (string dll in Directory.EnumerateFiles(
                     root, "*.dll", SearchOption.AllDirectories))
        {
            // Avoid scanning generated build/output files.
            string relative = Path.GetRelativePath(root, dll);

            string[] folders = relative.Split(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            );

            if (folders.Any(f =>
                    f.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    f.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                    f.Equals("Output", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            try
            {
                using var assembly = AssemblyDefinition.ReadAssembly(
                    dll,
                    new ReaderParameters
                    {
                        ReadingMode = ReadingMode.Deferred,
                        ReadSymbols = false
                    });

                foreach (var reference in assembly.MainModule.AssemblyReferences)
                {
                    if (!reference.Name.StartsWith(
                            "Microsoft.Xna.Framework",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string version = GetXnaVersion(reference.Version);

                    results.Add(new XnaDetection(
                        Path.GetFileName(dll),
                        version,
                        reference.Name,
                        reference.Version
                    ));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Skipping {Path.GetFileName(dll)}: {ex.Message}");
            }
        }

        return results;
    }

    private static string GetXnaVersion(Version version)
    {
        return version.Major switch
        {
            2 => "2.0",
            3 when version.Minor == 0 => "3.0 or 3.1",
            3 => "3.x",
            4 => "4.0",
            _ => $"Unknown ({version})"
        };
    }

    public static string DetectGameVersion(string root)
    {
        var versions = Detect(root)
            .Select(x => x.XnaVersion)
            .Distinct()
            .ToList();

        if (versions.Count == 0)
            return "Unknown";

        if (versions.Count == 1)
            return versions[0];

        return "Mixed (" + string.Join(", ", versions) + ")";
    }
}
