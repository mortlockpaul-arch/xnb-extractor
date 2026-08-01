using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using XnbExtractor.Readers;
using XnbExtractor.Xnb;

namespace XnbExtractor;

public class LoaderGame : Game
{
    private readonly GraphicsDeviceManager graphics;
    public ContentManager Assets { get; private set; } = null!;

    private readonly string inputFile;

    public LoaderGame(string inputFile)
    {
        Console.WriteLine("Attempting to load asset: " + inputFile);
        this.inputFile = inputFile;
        graphics = new GraphicsDeviceManager(this);
        Console.WriteLine("LoaderGame initialized.");
        Window.AllowUserResizing = false;
        IsMouseVisible = false;
        LoadContent();
    }
    protected override void LoadContent()
    {
        try
        {
            Console.WriteLine("Attempting to load asset: " + inputFile);

            var xnb = new XnbFile(inputFile);
            var data = xnb.Decompress();
            var xnbContent = XnbExtractor.Content.XnbContentReader.Parse(data);

            string readerType =
                xnbContent.Readers[xnbContent.PrimaryReaderIndex - 1];

            Console.WriteLine($"Reader: {readerType}");

            switch (readerType)
            {
                case "Microsoft.Xna.Framework.Content.Texture2DReader":
                    {
                        var texture = Content.Load<Texture2D>(inputFile);
                        Console.WriteLine($"Texture {texture.Width}x{texture.Height}");
                        break;
                    }

                case "Microsoft.Xna.Framework.Content.SongReader":
                    {
                        var song = Content.Load<Microsoft.Xna.Framework.Media.Song>(inputFile);
                        Console.WriteLine("Song loaded");
                        break;
                    }

                case "Microsoft.Xna.Framework.Content.SoundEffectReader":
                    {
                        var sound = Content.Load<Microsoft.Xna.Framework.Audio.SoundEffect>(inputFile);
                        Console.WriteLine("SoundEffect loaded");
                        break;
                    }

                default:
                    Console.WriteLine($"Unsupported reader: {readerType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        Exit();
    }
    protected override void Initialize()
    {
        Assets = new ContentManager(Services);

        base.Initialize();
    }

    protected override void Draw(GameTime gameTime)
    {
        Exit();
    }
}