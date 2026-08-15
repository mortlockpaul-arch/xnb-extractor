using System.IO;
using System.Text.Json;
using XnbExtractor.Models;

public static class EdgePalWriter
{
    public static void Write(XnbTEdgePal palette, string output)
    {
        var json = JsonSerializer.Serialize(
            palette,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(output, json);
    }
}