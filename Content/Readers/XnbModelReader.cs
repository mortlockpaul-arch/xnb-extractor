using System;
using System.Collections.Generic;
using System.Text;
using XnbExtractor.Content;
using XnbExtractor.Xnb;

namespace XnbExtractor.Readers;

public class XnbModelReader : IXnbTypeReader
{
    public bool CanDeserializeIntoExistingObject => throw new NotImplementedException();

    public object Read(XnbContentReader reader)
    {
        Console.WriteLine("===== SpriteFontReader =====");
        Console.WriteLine("Reading Model");

        // TODO: implement model parsing

        return new XnbExtractor.Content.XnbModel();
    }
}