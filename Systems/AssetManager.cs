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
    FuelBarrel,
    RepairGun,
    FuelPortClosed,
    FuelPortOpen,
    FuelPortLit,
    FuelButtonIdle,
    FuelButtonPressed,
    Throttle,
    AutoPickupModule,
    SolarPanelPowered,
    SolarPanelUnpowered,
    ApolloLowerHalf,
    ApolloUpperHalf,
    Background,
    BackgroundDay,
    BackgroundNight,
    Sandstorm,
    ProfileHouston,
    ProfilePlayer,
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
        textures[Art.FuelBarrel] = content.Load<Texture2D>("FuelBarrel");
        textures[Art.RepairGun] = content.Load<Texture2D>("repair");
        textures[Art.FuelPortClosed] = content.Load<Texture2D>("modules/fuel_port/fuel_port1");
        textures[Art.FuelPortOpen] = content.Load<Texture2D>("modules/fuel_port/fuel_port2");
        textures[Art.FuelPortLit] = content.Load<Texture2D>("modules/fuel_port/fuel_port3");
        textures[Art.FuelButtonIdle] = content.Load<Texture2D>("modules/button/button1");
        textures[Art.FuelButtonPressed] = content.Load<Texture2D>("modules/button/button2");
        textures[Art.Throttle] = content.Load<Texture2D>("modules/throttle");
        textures[Art.AutoPickupModule] = content.Load<Texture2D>("modules/suck");
        textures[Art.SolarPanelPowered] = content.Load<Texture2D>("modules/solar/solarpanel1");
        textures[Art.SolarPanelUnpowered] = content.Load<Texture2D>("modules/solar/solarpanel2");
        textures[Art.ApolloLowerHalf] = content.Load<Texture2D>("apollolowerhalf");
        textures[Art.ApolloUpperHalf] = content.Load<Texture2D>("apolloupperhalf");
        textures[Art.Background] = content.Load<Texture2D>("background/background_day");
        textures[Art.BackgroundDay] = content.Load<Texture2D>("background/background_day");
        textures[Art.BackgroundNight] = content.Load<Texture2D>("background/background_night");
        textures[Art.Sandstorm] = content.Load<Texture2D>("weather/sandstorm");
        textures[Art.ProfileHouston] = content.Load<Texture2D>("profile/houston");
        textures[Art.ProfilePlayer] = content.Load<Texture2D>("profile/player");
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
