using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XnbExtractor.Content;
using XnbExtractor.Models;

namespace XnbExtractor.Content.Readers
{
    internal class SpriteFontReader : IXnbTypeReader
    {
        public object Read(XnbContentReader reader)
        {
            var texture = reader.ReadObject();

            var glyphs = reader.ReadObject<List<Rectangle>>();
            var cropping = reader.ReadObject<List<Rectangle>>();
            var characters = reader.ReadObject<List<char>>();

            int lineSpacing = reader.ReadInt32();
            float spacing = reader.ReadSingle();

            var kerning = reader.ReadObject<List<Vector3>>();

            char? defaultCharacter = null;

            if (reader.ReadBoolean())
                defaultCharacter = reader.ReadChar();

            return new SpriteFontData(
                texture,
                glyphs,
                cropping,
                characters,
                kerning,
                lineSpacing,
                spacing,
                defaultCharacter);
        }
    }

}
