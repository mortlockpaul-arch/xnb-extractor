using XnbExtractor.Content;

namespace XnbExtractor.Content.Readers;

public class StringReader : IXnbTypeReader
{
    public object Read(XnbContentReader reader)
    {
        return reader.ReadString();
    }
}