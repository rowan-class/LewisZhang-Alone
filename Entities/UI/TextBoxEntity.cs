using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class DialogueLine
{
    public string Speaker { get; set; }
    public string Text { get; set; }
}

public class TextBoxEntity : Entity
{
    private const int BoxMargin = 32;
    private const int BoxHeight = 178;
    private const int PortraitSize = 126;
    private const int Padding = 18;
    private const int TextLineSpacing = 8;

    private readonly List<DialogueLine> _lines = new();
    private int _lineIndex;

    public bool IsActiveDialogue => _lines.Count > 0 && _lineIndex < _lines.Count;

    public TextBoxEntity()
        : base(Vector2.Zero, Point.Zero)
    {
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsActiveDialogue)
        {
            return;
        }

        if (ServiceLocator.Input.IsActionPressed(Action.Interact)
            || ServiceLocator.Input.IsActionPressed(Action.StartGame)
            || ServiceLocator.Input.IsActionPressed(Action.Jump))
        {
            _lineIndex++;
            if (_lineIndex >= _lines.Count)
            {
                _lines.Clear();
                _lineIndex = 0;
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActiveDialogue)
        {
            return;
        }

        DialogueLine line = _lines[_lineIndex];
        Rectangle boxBounds = new(
            BoxMargin,
            WorldConfig.ScreenHeight - BoxHeight - BoxMargin,
            WorldConfig.ScreenWidth - BoxMargin * 2,
            BoxHeight);

        Texture2D pixel = AssetManager.GetTexture(Art.pixel);
        spriteBatch.Draw(pixel, boxBounds, new Color(12, 16, 24, 235));
        DrawBorder(spriteBatch, boxBounds, Color.White);

        Rectangle portraitBounds = new(
            boxBounds.X + Padding,
            boxBounds.Y + Padding,
            PortraitSize,
            PortraitSize);
        spriteBatch.Draw(AssetManager.GetTexture(GetProfileArt(line.Speaker)), portraitBounds, Color.White);

        Vector2 namePosition = new(portraitBounds.Right + Padding, boxBounds.Y + Padding);
        spriteBatch.DrawString(AssetManager.ArialFont, GetSpeakerDisplayName(line.Speaker), namePosition, Color.LightSkyBlue);

        Rectangle textBounds = new(
            portraitBounds.Right + Padding,
            boxBounds.Y + Padding + 34,
            boxBounds.Right - portraitBounds.Right - Padding * 2,
            boxBounds.Bottom - boxBounds.Y - Padding * 2 - 34);

        DrawWrappedText(spriteBatch, line.Text ?? string.Empty, textBounds, Color.White);
    }

    public override Rectangle GetBounds()
    {
        return Rectangle.Empty;
    }

    public override IEnumerable<Rectangle> GetDebugRectangles()
    {
        yield break;
    }

    public void StartDialogue(IEnumerable<DialogueLine> lines)
    {
        _lines.Clear();
        if (lines != null)
        {
            foreach (DialogueLine line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line?.Text))
                {
                    _lines.Add(line);
                }
            }
        }

        _lineIndex = 0;
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        Texture2D pixel = AssetManager.GetTexture(Art.pixel);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, 2), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Bottom - 2, bounds.Width, 2), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, 2, bounds.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - 2, bounds.Top, 2, bounds.Height), color);
    }

    private static void DrawWrappedText(SpriteBatch spriteBatch, string text, Rectangle bounds, Color color)
    {
        string[] words = text.Split(' ');
        string currentLine = string.Empty;
        float y = bounds.Y;
        float lineHeight = AssetManager.ArialFont.LineSpacing + TextLineSpacing;

        foreach (string word in words)
        {
            string candidate = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            if (AssetManager.ArialFont.MeasureString(candidate).X <= bounds.Width)
            {
                currentLine = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                spriteBatch.DrawString(AssetManager.ArialFont, currentLine, new Vector2(bounds.X, y), color);
                y += lineHeight;
            }

            currentLine = word;
            if (y + lineHeight > bounds.Bottom)
            {
                return;
            }
        }

        if (!string.IsNullOrEmpty(currentLine) && y + lineHeight <= bounds.Bottom)
        {
            spriteBatch.DrawString(AssetManager.ArialFont, currentLine, new Vector2(bounds.X, y), color);
        }
    }

    private static Art GetProfileArt(string speaker)
    {
        return string.Equals(speaker, "houston", System.StringComparison.OrdinalIgnoreCase)
            ? Art.ProfileHouston
            : Art.ProfilePlayer;
    }

    private static string GetSpeakerDisplayName(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
        {
            return "Player";
        }

        return speaker.Trim();
    }
}
