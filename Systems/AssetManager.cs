using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public enum Art
{
    Player1,
    Player2,
    Player3,
    Player4,
    Player5,
    Vehicle,
    FuelPortClosed,
    FuelPortOpen,
    FuelPortLit,
    FuelButtonIdle,
    FuelButtonPressed,
    Throttle,
    Background,
    Button,
    pixel
}

public static class AssetManager
{
    private static readonly Dictionary<Art, Texture2D> textures = new();
    public static SpriteFont ArialFont;

    public static void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        textures.Clear();

        textures[Art.Player1] = content.Load<Texture2D>("player/player1");
        textures[Art.Player2] = content.Load<Texture2D>("player/player2");
        textures[Art.Player3] = content.Load<Texture2D>("player/player3");
        textures[Art.Player4] = content.Load<Texture2D>("player/player4");
        textures[Art.Player5] = content.Load<Texture2D>("player/player5");
        textures[Art.Vehicle] = content.Load<Texture2D>("Vehicle");
        textures[Art.FuelPortClosed] = content.Load<Texture2D>("modules/fuel_port/fuel_port1");
        textures[Art.FuelPortOpen] = content.Load<Texture2D>("modules/fuel_port/fuel_port2");
        textures[Art.FuelPortLit] = content.Load<Texture2D>("modules/fuel_port/fuel_port3");
        textures[Art.FuelButtonIdle] = content.Load<Texture2D>("modules/button/button1");
        textures[Art.FuelButtonPressed] = content.Load<Texture2D>("modules/button/button2");
        textures[Art.Throttle] = content.Load<Texture2D>("modules/throttle");
        textures[Art.Background] = content.Load<Texture2D>("background");
        textures[Art.Button] = CreateSolidTexture(graphicsDevice, 120, 40, Color.LightGray);
        textures[Art.pixel] = CreateSolidTexture(graphicsDevice, 1, 1, Color.White);
        ArialFont = content.Load<SpriteFont>("Arial");
    }

    private static Texture2D CreateSolidTexture(GraphicsDevice graphicsDevice, int width, int height, Color color)
    {
        Texture2D texture = new(graphicsDevice, width, height);
        Color[] data = new Color[width * height];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = color;
        }

        texture.SetData(data);
        return texture;
    }

    public static Texture2D GetTexture(Art art)
    {
        return textures[art];
    }
}
