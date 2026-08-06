using System;
using System.Collections.Generic;
using System.Text;
using XnbExtractor.Xnb;

namespace XnbExtractor.Readers;

public static class XnbModelReader
{
    public static object Read(
        XnbExtractor.Content.XnbContentReader reader)
    {
        Console.WriteLine("Reading Model");

        // TODO: implement model parsing

        return new XnbExtractor.Content.XnbModel();
    }
}