using XnbExtractor.Content;

namespace XnbExtractor.Content.Readers;

public class XnbInt32Reader : IXnbTypeReader
{
    public object Read(XnbContentReader reader)
    {
        return reader.ReadInt32();
    }
}