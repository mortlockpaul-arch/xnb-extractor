namespace XnbExtractor.Models
{
    public class XnbTBmp256
    {
        public int Width { get; private set; }

        public int Height { get; private set; }

        public byte[] PalIdx { get; private set; }

        public XnbTBmp256(int width, int height, byte[] palIdx)
        {
            this.Width = width;
            this.Height = height;
            this.PalIdx = palIdx;
        }
    }
}