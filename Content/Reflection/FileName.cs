using System;
using System.Linq;
using System.Reflection;

namespace XnbExtractor.Content.Reflection;

internal static class RuntimeReaderFactory
{
    private static readonly Assembly RuntimeAssembly =
        AppDomain.CurrentDomain.GetAssemblies()
            .First(a => a.GetName().Name == "isdf.Runtime");

    public static object? ReadType(XnbContentReader reader, string typeName)
    {
        var shortName = typeName.Split(',')[0].Trim();

        return shortName switch
        {
            "isdf.Runtime.TEdgePalReader" => ReadTEdgePal(reader),
            "isdf.Runtime.TBmp256Reader" => ReadTBmp256(reader),
            "isdf.Runtime.TCSVtoArrayReader" => ReadTCSVtoArray(reader),
            "isdf.Runtime.TextMessageReader" => ReadTextMessage(reader),
            "isdf.Runtime.TEventScriptSheetReader" => ReadTEventScriptSheet(reader),
            _ => null
        };
    }

    private static object ReadTEdgePal(XnbContentReader reader)
    {
        uint[] colors = new uint[256];

        for (int i = 0; i < 256; i++)
            colors[i] = reader.ReadUInt32();

        var type = RuntimeAssembly.GetType("isdf.Runtime.TEdgePal")!;
        return Activator.CreateInstance(type, colors)!;
    }

    private static object ReadTBmp256(XnbContentReader reader)
    {
        int width = reader.ReadInt32();
        int height = reader.ReadInt32();
        byte[] palIdx = reader.ReadBytes(width * height);

        var type = RuntimeAssembly.GetType("isdf.Runtime.TBmp256")!;

        return Activator.CreateInstance( type, width, height, palIdx)!;
    }

    private static object ReadTCSVtoArray(XnbContentReader reader)
    {
        int count = reader.ReadInt32();
        var type = RuntimeAssembly.GetType("isdf.Runtime.TCSVtoArray")!;
        var array = Array.CreateInstance(type, count);

        for (int i = 0; i < count; i++)
            array.SetValue(
                CreateRuntimeObject(
                    "isdf.Runtime.TCSVtoArray",
                    reader.ReadObject<string[]>(),
                    reader.ReadObject<int[]>()),
                i);

        return array;
    }

    private static void SetField(object obj, string name, object? value)
    {
        obj.GetType().GetField(
            name,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic)!
            .SetValue(obj, value);
    }

    private static object CreateRuntimeObject(string typeName, params object?[] args)
    {
        var type = RuntimeAssembly.GetType(typeName)
            ?? throw new InvalidOperationException(
                $"Runtime type not found: {typeName}");

        return Activator.CreateInstance(type, args)
            ?? throw new InvalidOperationException(
                $"Could not create runtime type: {typeName}");
    }

    private static object ReadTextMessage(XnbContentReader reader)
    {
        var type = RuntimeAssembly.GetType("isdf.Runtime.TextMessage")!;
        var message = Activator.CreateInstance(type)!;

        SetField(message, "Font", reader.ReadObject());
        SetField(message, "Message", reader.ReadObject<string[]>());

        return message;
    }

    private static object ReadTEventScriptSheet(XnbContentReader reader)
    {
        var header = CreateRuntimeObject(
            "isdf.Runtime.TEventScriptHeader",
            reader.ReadObject<string[]>(),
            reader.ReadObject<int[]>(),
            reader.ReadObject<int[]>(),
            reader.ReadObject<string[]>());

        int count = reader.ReadInt32();

        var lineType = RuntimeAssembly.GetType(
            "isdf.Runtime.TEventScriptLine")!;

        var lines = Array.CreateInstance(lineType, count);

        for (int i = 0; i < count; i++)
        {
            lines.SetValue(
                CreateRuntimeObject(
                    "isdf.Runtime.TEventScriptLine",
                    reader.ReadObject<string[]>(),
                    reader.ReadObject<int[]>(),
                    reader.ReadObject<int[]>(),
                    reader.ReadObject<int[]>(),
                    reader.ReadObject<int[]>(),
                    reader.ReadObject<string>()),
                i);
        }

        return CreateRuntimeObject(
            "isdf.Runtime.TEventScriptSheet",
            header,
            lines);
    }
}
