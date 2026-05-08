using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class Scene
{
    private readonly List<Entity> _entities = new();

    public bool finished = false;
    public string nextScene = "";

    public virtual void Open()
    {
    }

    public virtual void Close()
    {
        finished = false;
        nextScene = "";
    }

    public void AddEntity(Entity entity)
    {
        _entities.Add(entity);
        entity.SetScene(this);
    }

    public void RemoveEntity(Entity entity)
    {
        _entities.Remove(entity);
        entity.RemoveFromScene(this);
        entity.SetScene(null);
    }

    public T FindFirstEntity<T>() where T : Entity
    {
        foreach (Entity entity in _entities)
        {
            if (entity is T typedEntity)
            {
                return typedEntity;
            }
        }

        return null;
    }

    public IEnumerable<T> FindEntities<T>() where T : Entity
    {
        foreach (Entity entity in _entities)
        {
            if (entity is T typedEntity)
            {
                yield return typedEntity;
            }
        }
    }

    public virtual void Update(GameTime gameTime)
    {
        for (int i = _entities.Count - 1; i >= 0; i--)
        {
            Entity entity = _entities[i];
            if (!entity.IsActive)
            {
                _entities.RemoveAt(i);
                entity.RemoveFromScene(this);
                entity.SetScene(null);
                continue;
            }

            entity.Update(gameTime);
        }
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawEntities(spriteBatch, _entities);
        if (Game1.Debug)
        {
            DrawDebugRectangles(spriteBatch, GetDebugRectangles());
        }
        spriteBatch.End();
    }

    public void ChangeScene(string nextSceneKey)
    {
        finished = true;
        nextScene = nextSceneKey;
    }

    protected void DrawEntities(SpriteBatch spriteBatch, IEnumerable<Entity> entities)
    {
        foreach (Entity entity in entities)
        {
            entity.Draw(spriteBatch);
        }
    }

    protected virtual IEnumerable<Rectangle> GetDebugRectangles()
    {
        foreach (Entity entity in _entities)
        {
            foreach (Rectangle rect in entity.GetDebugRectangles())
            {
                yield return rect;
            }
        }
    }

    protected void DrawDebugRectangles(SpriteBatch spriteBatch, IEnumerable<Rectangle> rectangles)
    {
        foreach (Rectangle rect in rectangles)
        {
            DrawDebugRectangle(spriteBatch, rect, Color.Red);
        }
    }

    protected void DrawDebugRectangle(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        if (bounds == Rectangle.Empty || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        Texture2D pixel = AssetManager.GetTexture(Art.pixel);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Bottom - 1, bounds.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, 1, bounds.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - 1, bounds.Top, 1, bounds.Height), color);
    }

    protected void DrawInfoPanel(SpriteBatch spriteBatch, string title, IReadOnlyList<HudLine> lines)
    {
        if (string.IsNullOrWhiteSpace(title) || lines == null || lines.Count == 0)
        {
            return;
        }

        const float titleScale = 0.88f;
        const float lineScale = 0.76f;
        const int paddingX = 14;
        const int paddingY = 12;
        const int lineHeight = 23;
        const int titleHeight = 24;
        const int headerGap = 9;
        const int maxPanelWidth = 760;

        SpriteFont font = AssetManager.ArialFont;
        int contentWidth = (int)MathF.Ceiling(font.MeasureString(title).X * titleScale);
        foreach (HudLine line in lines)
        {
            contentWidth = Math.Max(contentWidth, (int)MathF.Ceiling(font.MeasureString(line.Text).X * lineScale));
        }

        int panelWidth = Math.Min(maxPanelWidth, contentWidth + paddingX * 2);
        int panelHeight = paddingY * 2 + titleHeight + headerGap + lines.Count * lineHeight;
        Rectangle panel = new(16, 16, panelWidth, panelHeight);

        Texture2D pixel = AssetManager.GetTexture(Art.pixel);
        spriteBatch.Draw(pixel, new Rectangle(panel.X + 4, panel.Y + 4, panel.Width, panel.Height), Color.Black * 0.35f);
        spriteBatch.Draw(pixel, panel, new Color(15, 19, 23, 214));
        spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Y, 4, panel.Height), new Color(108, 199, 217, 238));
        spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Y, panel.Width, 1), Color.White * 0.22f);
        spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Bottom - 1, panel.Width, 1), Color.Black * 0.45f);
        spriteBatch.Draw(pixel, new Rectangle(panel.X + paddingX, panel.Y + paddingY + titleHeight + 2, panel.Width - paddingX * 2, 1), Color.White * 0.16f);

        Vector2 titlePosition = new(panel.X + paddingX, panel.Y + paddingY);
        spriteBatch.DrawString(font, title, titlePosition + new Vector2(1f, 1f), Color.Black * 0.6f, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, title, titlePosition, new Color(224, 244, 248), 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

        float y = panel.Y + paddingY + titleHeight + headerGap;
        foreach (HudLine line in lines)
        {
            Vector2 position = new(panel.X + paddingX, y);
            spriteBatch.DrawString(font, line.Text, position + new Vector2(1f, 1f), Color.Black * 0.55f, 0f, Vector2.Zero, lineScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, line.Text, position, line.Color, 0f, Vector2.Zero, lineScale, SpriteEffects.None, 0f);
            y += lineHeight;
        }
    }

    protected void DrawSimpleDebugText(SpriteBatch spriteBatch, string title, IReadOnlyList<HudLine> lines)
    {
        if (string.IsNullOrWhiteSpace(title) || lines == null || lines.Count == 0)
        {
            return;
        }

        const float scale = 1f;
        const int lineHeight = 30;

        SpriteFont font = AssetManager.ArialFont;
        Vector2 position = new(16f, 14f);

        spriteBatch.DrawString(font, title, position + Vector2.One, Color.Black * 0.7f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, title, position, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        position.Y += lineHeight;

        foreach (HudLine line in lines)
        {
            spriteBatch.DrawString(font, line.Text, position + Vector2.One, Color.Black * 0.7f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, line.Text, position, line.Color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            position.Y += lineHeight;
        }
    }

    protected void DrawPausePage(SpriteBatch spriteBatch, string subtitle, IReadOnlyList<HudLine> controls)
    {
        if (controls == null || controls.Count == 0)
        {
            return;
        }

        const int panelWidth = 940;
        const int paddingX = 34;
        const int paddingY = 28;
        const int titleHeight = 46;
        const int subtitleHeight = 34;
        const int lineHeight = 36;
        const float titleScale = 1.3f;
        const float subtitleScale = 1f;
        const float lineScale = 1f;

        int panelHeight = paddingY * 2 + titleHeight + subtitleHeight + controls.Count * lineHeight + 24;
        Rectangle screen = new(0, 0, WorldConfig.ScreenWidth, WorldConfig.ScreenHeight);
        Rectangle panel = new(
            (WorldConfig.ScreenWidth - panelWidth) / 2,
            (WorldConfig.ScreenHeight - panelHeight) / 2,
            panelWidth,
            panelHeight);

        Texture2D pixel = AssetManager.GetTexture(Art.pixel);
        SpriteFont font = AssetManager.ArialFont;
        spriteBatch.Draw(pixel, screen, Color.Black * 0.56f);
        spriteBatch.Draw(pixel, new Rectangle(panel.X + 7, panel.Y + 7, panel.Width, panel.Height), Color.Black * 0.35f);
        spriteBatch.Draw(pixel, panel, new Color(13, 17, 21, 236));
        spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Y, panel.Width, 3), new Color(108, 199, 217, 245));
        spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Bottom - 1, panel.Width, 1), Color.White * 0.16f);

        Vector2 titleSize = font.MeasureString("PAUSED") * titleScale;
        Vector2 titlePosition = new(panel.Center.X - titleSize.X / 2f, panel.Y + paddingY);
        spriteBatch.DrawString(font, "PAUSED", titlePosition + new Vector2(2f, 2f), Color.Black * 0.6f, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, "PAUSED", titlePosition, new Color(224, 244, 248), 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

        Vector2 subtitleSize = font.MeasureString(subtitle) * subtitleScale;
        Vector2 subtitlePosition = new(panel.Center.X - subtitleSize.X / 2f, titlePosition.Y + titleHeight);
        spriteBatch.DrawString(font, subtitle, subtitlePosition, new Color(188, 214, 218), 0f, Vector2.Zero, subtitleScale, SpriteEffects.None, 0f);

        int separatorY = panel.Y + paddingY + titleHeight + subtitleHeight + 4;
        spriteBatch.Draw(pixel, new Rectangle(panel.X + paddingX, separatorY, panel.Width - paddingX * 2, 1), Color.White * 0.18f);

        float y = separatorY + 18;
        foreach (HudLine control in controls)
        {
            Vector2 position = new(panel.X + paddingX, y);
            spriteBatch.DrawString(font, control.Text, position + new Vector2(1f, 1f), Color.Black * 0.6f, 0f, Vector2.Zero, lineScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, control.Text, position, control.Color, 0f, Vector2.Zero, lineScale, SpriteEffects.None, 0f);
            y += lineHeight;
        }
    }

    protected void DrawPauseHint(SpriteBatch spriteBatch)
    {
        const string text = "Esc  Pause";
        const float scale = 1f;
        const int paddingX = 12;
        const int paddingY = 9;

        SpriteFont font = AssetManager.ArialFont;
        Vector2 textSize = font.MeasureString(text) * scale;
        Rectangle bounds = new(
            WorldConfig.ScreenWidth - (int)MathF.Ceiling(textSize.X) - paddingX * 2 - 16,
            16,
            (int)MathF.Ceiling(textSize.X) + paddingX * 2,
            (int)MathF.Ceiling(textSize.Y) + paddingY * 2);

        Texture2D pixel = AssetManager.GetTexture(Art.pixel);
        spriteBatch.Draw(pixel, bounds, new Color(15, 19, 23, 188));
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, 3, bounds.Height), new Color(108, 199, 217, 220));

        Vector2 position = new(bounds.X + paddingX, bounds.Y + paddingY);
        spriteBatch.DrawString(font, text, position + new Vector2(1f, 1f), Color.Black * 0.55f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, text, position, new Color(224, 244, 248), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}

public readonly record struct HudLine(string Text, Color Color);
