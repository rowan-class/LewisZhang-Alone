using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public enum TextAlignment
{
    TopLeft,
    TopCenter
}

public class TextEntity : Entity
{
    protected string _text;
    private readonly TextAlignment _alignment;
    private Color _color = Color.White;

    public TextEntity(Vector2 position, string text, TextAlignment alignment = TextAlignment.TopLeft)
        : base(position, Point.Zero)
    {
        _text = text;
        _alignment = alignment;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Vector2 drawPosition = _position;
        if (_alignment == TextAlignment.TopCenter)
        {
            Vector2 textSize = AssetManager.ArialFont.MeasureString(_text);
            drawPosition.X -= textSize.X / 2f;
        }

        spriteBatch.DrawString(AssetManager.ArialFont, _text, drawPosition, _color);
    }

    public override Rectangle GetBounds()
    {
        return Rectangle.Empty;
    }

    public override System.Collections.Generic.IEnumerable<Rectangle> GetDebugRectangles()
    {
        yield break;
    }

    public void SetText(string newText)
    {
        _text = newText;
    }

    public void SetColor(Color color)
    {
        _color = color;
    }
}
