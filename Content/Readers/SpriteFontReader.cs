using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using XnbExtractor.Content;
using XnbExtractor.Models;

namespace XnbExtractor.Content.Readers
{
    internal class SpriteFontReader : IXnbTypeReader
    {
        public object Read(XnbContentReader reader)
        {
            var font = new XnbSpriteFont();
            Console.WriteLine(
    $"SpriteFont reader position: {reader.Position}"
);

            byte[] peek = reader.ReadBytes(32);

            Console.WriteLine(
                BitConverter.ToString(peek)
            );

            reader.Seek(reader.Position - 32);
            font.Texture =
                (XnbTexture)reader.ReadObject();


            int glyphCount = reader.ReadInt32();
            Console.WriteLine($"Glyph count = {glyphCount}");
            for (int i = 0; i < glyphCount; i++)
            {
                font.Glyphs.Add(
                    ReadRectangle(reader)
                );
            }


            int croppingCount = reader.ReadInt32();
            Console.WriteLine($"Cropping count = {croppingCount}");
            for (int i = 0; i < croppingCount; i++)
            {
                font.Cropping.Add(
                    ReadRectangle(reader)
                );
            }


            int characterCount = reader.ReadInt32();
            Console.WriteLine($"Character count = {characterCount}");
            for (int i = 0; i < characterCount; i++)
            {
                font.Characters.Add(
                    reader.ReadChar()
                );
            }


            font.Spacing =
                reader.ReadSingle();


            int kerningCount =
                reader.ReadInt32();

            for (int i = 0; i < kerningCount; i++)
            {
                font.Kerning.Add(
                    ReadVector3(reader)
                );
            }


            return font;
        }

        private Vector3 ReadVector3(XnbContentReader reader)
        {
            throw new NotImplementedException();
        }

        private Rectangle ReadRectangle(XnbContentReader reader)
        {
            return new Rectangle
            {
                X = reader.ReadInt32(),
                Y = reader.ReadInt32(),
                Width = reader.ReadInt32(),
                Height = reader.ReadInt32()
            };
        }
    }
}
