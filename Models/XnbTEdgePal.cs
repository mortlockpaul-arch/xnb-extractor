namespace XnbExtractor.Models;

public class XnbTEdgePal
{
    public uint[] PalARGB { get; }

    public XnbTEdgePal(uint[] colors)
    {
        PalARGB = colors;
    }
}
