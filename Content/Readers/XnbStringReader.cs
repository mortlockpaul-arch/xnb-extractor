using XnbExtractor.Content;

namespace XnbExtractor.Content.Readers;

public class XnbStringReader : IXnbTypeReader
{
    public object Read(XnbContentReader reader)
    {
        return reader.ReadString();
    }
}