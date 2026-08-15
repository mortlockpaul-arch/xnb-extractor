using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XnbExtractor.Content;

namespace XnbExtractor.Models
{
    internal class SpriteFontData
    {
        public object? Texture { get; }
        public List<RectangleData> Glyphs { get; }
        public List<RectangleData> Cropping { get; }
        public List<char> Characters { get; }
        public List<Vector3> Kerning { get; }
        public int LineSpacing { get; }
        public float Spacing { get; }
        public char? DefaultCharacter { get; }

        public SpriteFontData(
            object? texture,
            List<Rectangle> glyphs,
            List<Rectangle> cropping,
            List<char> characters,
            List<Vector3> kerning,
            int lineSpacing,
            float spacing,
            char? defaultCharacter)
        {
            Texture = texture;
            Glyphs = glyphs.ConvertAll(r => new RectangleData(r));
            Cropping = cropping.ConvertAll(r => new RectangleData(r));
            Characters = characters;
            Kerning = kerning;
            LineSpacing = lineSpacing;
            Spacing = spacing;
            DefaultCharacter = defaultCharacter;
        }
    }

    public class RectangleData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public RectangleData(Rectangle rectangle)
        {
            X = rectangle.X;
            Y = rectangle.Y;
            Width = rectangle.Width;
            Height = rectangle.Height;
        }
    }
}
