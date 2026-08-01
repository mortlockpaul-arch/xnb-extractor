using System;
using System.Collections.Generic;
using System.Text;

namespace XnbExtractor.Content;

public class XnbContent
{

    public List<string> Readers { get; } = new();

    public int SharedResourceCount { get; set; }

    public int PrimaryReaderIndex { get; set; }

    public long DataPosition { get; set; }

    public string PrimaryReaderType =>
        Readers[PrimaryReaderIndex - 1];
}