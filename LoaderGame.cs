using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using System;
using XnbExtractor.Content;

namespace XnbExtractor
{
    internal class LoaderGame: IDisposable
    {
        private string inputFile;

        public LoaderGame(string inputFile, Program.XnbCore.LoadedAsset loaded)
        {
            this.inputFile = inputFile;
            this.LoadedAsset = loaded;
            var game = new Game();
            if (loaded.Asset == null)
            {
                throw new NotImplementedException($"Asset {inputFile} is not supported.");
            }
            switch (loaded.Asset.GetType().FullName)
            {
                case "Microsoft.Xna.Framework.Media.Song":
                    game.Content.Load<Song>(inputFile);
                    break;
                case "Microsoft.Xna.Framework.Graphics.Texture2D":
                    game.Content.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(inputFile);
                    break;
                case "Microsoft.Xna.Framework.Graphics.Effect":
                    game.Content.Load<Microsoft.Xna.Framework.Graphics.Effect>(inputFile);
                    break;
                case "Microsoft.Xna.Framework.Audio.SoundEffect":
                    game.Content.Load<Microsoft.Xna.Framework.Audio.SoundEffect>(inputFile);
                    break;
                case "Microsoft.Xna.Framework.Graphics.SpriteFont":
                    game.Content.Load<Microsoft.Xna.Framework.Graphics.SpriteFont>(inputFile);
                    break;
                case "Microsoft.Xna.Framework.Graphics.Model":
                    game.Content.Load<Microsoft.Xna.Framework.Graphics.Model>(inputFile);
                    break;
                case "Microsoft.Xna.Framework.Graphics.Texture3D":
                    game.Content.Load<Microsoft.Xna.Framework.Graphics.Texture3D>(inputFile);
                    break;
                case "Microsoft.Xna.Framework.Graphics.TextureCube":
                    game.Content.Load<Microsoft.Xna.Framework.Graphics.TextureCube>(inputFile);
                    break;
                default:
                    throw new NotImplementedException($"Asset {inputFile} is not supported.");
            }
           
        }

        public Program.XnbCore.LoadedAsset LoadedAsset { get; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}