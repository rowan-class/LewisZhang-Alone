using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


namespace LewisZhang_Alone;

public enum Art
{
    Player,
    Player1,
    Player2,
    Spike,
    Alien,
    Button,
    pixel, 
    Enemy1, 
    Enemy2
}

public static class AssetManager
{
    private static readonly Dictionary<Art, Texture2D> textures = new();
    public static SpriteFont ArialFont;

    public static void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        textures.Clear();

        textures[Art.Player1] = content.Load<Texture2D>("player1");
        textures[Art.Player2] = content.Load<Texture2D>("player2");
        textures[Art.Player] = textures[Art.Player1];
        textures[Art.Spike] = content.Load<Texture2D>("spike");
        textures[Art.Alien] = content.Load<Texture2D>("alien");
        textures[Art.Button] = CreateSolidTexture(graphicsDevice, 120, 40, Color.LightGray);
        textures[Art.pixel] = CreateSolidTexture(graphicsDevice, 1, 1, Color.White);
        textures[Art.Enemy1] = CreateSolidTexture(graphicsDevice, 64, 64, Color.Green);
        textures[Art.Enemy2] = CreateSolidTexture(graphicsDevice, 64, 64, Color.Red);
        ArialFont = content.Load<SpriteFont>("Arial");
    }

    private static Texture2D CreateSolidTexture(GraphicsDevice graphicsDevice, int width, int height, Color color)
    {
        Texture2D texture = new(graphicsDevice, width, height);
        Color[] data = new Color[width * height];
        for (int i = 0; i < data.Length; i++)
            data[i] = color;
        texture.SetData(data);
        return texture;
    }

    public static Texture2D GetTexture(Art art)
    {
        return textures[art];
    }
}
