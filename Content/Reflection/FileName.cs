using isdf.Runtime;
using XnbExtractor.Models;

namespace XnbExtractor.Content.Reflection;

internal static class IsdfRuntimeReaderFactory
{
    public static object? ReadType(XnbContentReader reader, string typeName)
    {
        return typeName switch
        {
            "isdf.Runtime.TEdgePalReader,isdf.Runtime" => ReadTEdgePal(reader),
            "isdf.Runtime.TBmp256Reader,isdf.Runtime" => ReadTBmp256(reader),
            "isdf.Runtime.TCSVtoArrayReader,isdf.Runtime" => ReadTCSVtoArray(reader),
            "isdf.Runtime.TextMessageReader,isdf.Runtime" => ReadTextMessage(reader),
            "isdf.Runtime.TEventScriptSheetReader,isdf.Runtime" => ReadTEventScriptSheet(reader),
            _ => null
        };
    }

    private static object ReadTEdgePal(XnbContentReader reader)
    {
        uint[] colors = new uint[256];

        for (int i = 0; i < 256; i++)
            colors[i] = reader.ReadUInt32();

        return new XnbTEdgePal(colors);
    }

    private static object ReadTBmp256(XnbContentReader reader)
    {
        int width = reader.ReadInt32();
        int height = reader.ReadInt32();
        byte[] palIdx = reader.ReadBytes(width * height);

        return new XnbTBmp256(width, height, palIdx);
    }

    private static object ReadTCSVtoArray(XnbContentReader reader)
    {
        int count = reader.ReadInt32();
        var array = new XnbTCSVtoArray[count];

        for (int i = 0; i < count; i++)
        {
            string[] strings = reader.ReadObject<string[]>()!;
            int[] values = reader.ReadObject<int[]>()!;

            array[i] = new XnbTCSVtoArray(strings, values);
        }

        return array;
    }

    private static TextMessage ReadTextMessage(XnbContentReader reader)
    {
        return new TextMessage
        {
            Font = reader.ReadObject<Microsoft.Xna.Framework.Graphics.SpriteFont>(),
            Message = reader.ReadObject<string[]>()
        };
    }

    private static TEventScriptSheet ReadTEventScriptSheet(XnbContentReader reader)
    {
        var header = new TEventScriptHeader(
            reader.ReadObject<string[]>(),
            reader.ReadObject<int[]>(),
            reader.ReadObject<int[]>(),
            reader.ReadObject<string[]>());

        var lines = new TEventScriptLine[reader.ReadInt32()];

        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = new TEventScriptLine(
                reader.ReadObject<string[]>(),
                reader.ReadObject<int[]>(),
                reader.ReadObject<int[]>(),
                reader.ReadObject<int[]>(),
                reader.ReadObject<int[]>(),
                reader.ReadObject<string>());
        }

        return new TEventScriptSheet(header, lines);
    }
}