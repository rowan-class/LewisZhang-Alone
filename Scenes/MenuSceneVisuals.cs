using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public static class MenuSceneVisuals
{
    public static void DrawBackdrop(SpriteBatch spriteBatch, Art backgroundArt, int sourceX, Color overlay)
    {
        Texture2D background = AssetManager.GetTexture(backgroundArt);
        int sourceWidth = (int)MathF.Round(background.Height * (WorldConfig.ScreenWidth / (float)WorldConfig.ScreenHeight));
        sourceWidth = Math.Clamp(sourceWidth, 1, background.Width);
        sourceX = Math.Clamp(sourceX, 0, Math.Max(0, background.Width - sourceWidth));

        Rectangle source = new(sourceX, 0, sourceWidth, background.Height);
        Rectangle destination = new(0, 0, WorldConfig.ScreenWidth, WorldConfig.ScreenHeight);
        spriteBatch.Draw(background, destination, source, Color.White);
        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), destination, overlay);
    }

    public static void DrawPanel(SpriteBatch spriteBatch, Rectangle bounds, Color fill, Color border)
    {
        Texture2D pixel = AssetManager.GetTexture(Art.pixel);
        spriteBatch.Draw(pixel, bounds, fill);
        DrawLine(spriteBatch, bounds.Left, bounds.Top, bounds.Right, bounds.Top, border);
        DrawLine(spriteBatch, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1, border * 0.6f);
        DrawLine(spriteBatch, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom, border * 0.8f);
        DrawLine(spriteBatch, bounds.Right - 1, bounds.Top, bounds.Right - 1, bounds.Bottom, border * 0.8f);
    }

    public static void DrawCenteredText(SpriteBatch spriteBatch, string text, Vector2 centerTop, Color color, float scale)
    {
        Vector2 size = AssetManager.ArialFont.MeasureString(text) * scale;
        Vector2 position = new(centerTop.X - size.X / 2f, centerTop.Y);
        DrawText(spriteBatch, text, position, color, scale);
    }

    public static void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale)
    {
        spriteBatch.DrawString(AssetManager.ArialFont, text, position + new Vector2(2f, 2f), Color.Black * 0.75f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(AssetManager.ArialFont, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    public static void DrawDivider(SpriteBatch spriteBatch, int centerX, int y, int width, Color color)
    {
        int left = centerX - width / 2;
        DrawLine(spriteBatch, left, y, left + width, y, color);
        DrawLine(spriteBatch, left + width / 3, y + 4, left + width * 2 / 3, y + 4, color * 0.45f);
    }

    private static void DrawLine(SpriteBatch spriteBatch, int x1, int y1, int x2, int y2, Color color)
    {
        if (x1 == x2)
        {
            spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), new Rectangle(x1, Math.Min(y1, y2), 1, Math.Abs(y2 - y1)), color);
            return;
        }

        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), new Rectangle(Math.Min(x1, x2), y1, Math.Abs(x2 - x1), 1), color);
    }
}
