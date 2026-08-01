using System;
using System.Collections.Generic;
using System.Text;
using XnbExtractor.Xnb;

namespace XnbExtractor.Readers;

public static class ModelReader
{
    public static object Read(
        XnbExtractor.Content.XnbContent content,
        XnbExtractor.Content.XnbContentReader reader)
    {
        Console.WriteLine("Reading Model");
        for (int i = 0; i < content.Readers.Count; i++)
        {
            Console.WriteLine($"{i}: {content.Readers[i]}");
        }
        // Temporary - just consume/inspect data
        return new XnbExtractor.Content.XnbModel();
    }
}