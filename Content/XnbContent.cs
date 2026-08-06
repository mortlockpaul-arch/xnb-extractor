using System;
using System.Collections.Generic;
using XnbExtractor.Xnb;

namespace XnbExtractor.Content;

public class XnbContent
{
    //public TargetPlatform Platform { get; set; }

    public List<string> Readers { get; } = new();

    public int SharedResourceCount { get; set; }

    public int PrimaryReaderIndex { get; set; }

    public long DataPosition { get; set; }

    public string PrimaryReaderType
    {
        get
        {
            int index = PrimaryReaderIndex - 1;

            if (index < 0 || index >= Readers.Count)
            {
                throw new IndexOutOfRangeException(
                    $"Invalid primary reader index: {PrimaryReaderIndex}");
            }

            return Readers[index];
        }
    }
}