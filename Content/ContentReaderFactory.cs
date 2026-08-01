using System;
using XnbExtractor.Content;

namespace XnbExtractor.Readers
{
    public static class ContentReaderFactory
    {
    public static object Read(XnbContent content, XnbExtractor.Content.XnbContentReader reader)
        {
            Console.WriteLine(
                $"Reader requested: {content.PrimaryReaderType}"
            );

            switch (content.PrimaryReaderType)
            {
                case string s when s.Contains("Texture2D"):
                    return Texture2DReader.Parse(reader);

                case string s when s.Contains("ModelReader"):
                    return ModelReader.Read(content, reader);

                default:
                    throw new NotSupportedException(
                        $"Unsupported content reader: {content.PrimaryReaderType}"
                    );
            }
        }
    }
}