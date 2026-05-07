using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public static class BackgroundRenderer
{
    public static void DrawLooping(SpriteBatch spriteBatch, float scrollX, int width)
    {
        Texture2D day = AssetManager.GetTexture(Art.BackgroundDay);
        Texture2D night = AssetManager.GetTexture(Art.BackgroundNight);
        float nightBlend = MathHelper.Clamp(Game1.BackgroundNightBlend, 0f, 1f);

        DrawTextureLoop(spriteBatch, day, scrollX, width, Color.White * (1f - nightBlend));
        if (nightBlend > 0f)
        {
            DrawTextureLoop(spriteBatch, night, scrollX, width, Color.White * nightBlend);
        }
    }

    private static void DrawTextureLoop(SpriteBatch spriteBatch, Texture2D texture, float scrollX, int width, Color tint)
    {
        if (texture == null || tint.A <= 0)
        {
            return;
        }

        float wrappedOffset = scrollX % texture.Width;
        if (wrappedOffset < 0f)
        {
            wrappedOffset += texture.Width;
        }

        for (int x = -(int)MathF.Round(wrappedOffset); x < width; x += texture.Width)
        {
            spriteBatch.Draw(texture, new Rectangle(x, 0, texture.Width, WorldConfig.WorldHeight), tint);
        }
    }
}
