using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XnbExtractor.Content;

namespace XnbExtractor.Models
{
    public class XnbSpriteFont
    {
        public XnbTexture Texture { get; set; }

        public List<Rectangle> Glyphs { get; } = new();
        public List<Rectangle> Cropping { get; } = new();
        public List<char> Characters { get; } = new();
        public List<Vector3> Kerning { get; } = new();

        public int LineSpacing { get; set; }
        public float Spacing { get; set; }
        public char? DefaultCharacter { get; set; }
    }
}
