using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LewisZhang_Alone;

public class ButtonEntity : SpriteEntity
{
    private readonly string _buttonText;
    private readonly string _nextSceneKey;
    private readonly Func<bool> _isEnabled;
    private bool _hovered;

    public ButtonEntity(Art art, Vector2 position)
        : this(art, position, "Start", "level1")
    {
    }

    public ButtonEntity(Art art, Vector2 position, string buttonText, string nextSceneKey)
        : this(art, position, buttonText, nextSceneKey, null)
    {
    }

    public ButtonEntity(Art art, Vector2 position, string buttonText, string nextSceneKey, Func<bool> isEnabled)
        : base(art, position)
    {
        _buttonText = buttonText;
        _nextSceneKey = nextSceneKey;
        _isEnabled = isEnabled;

        Vector2 stringDisplaySize = AssetManager.ArialFont.MeasureString(_buttonText);
        _size = new Point(
            System.Math.Max(200, (int)stringDisplaySize.X + 20),
            System.Math.Max(60, (int)stringDisplaySize.Y + 20));
    }

    public override void Update(GameTime gameTime)
    {
        bool enabled = IsEnabled();
        _hovered = false;

        MouseState mouseState = Mouse.GetState();
        Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
        if (enabled && GetBounds().Contains(mousePosition))
        {
            _hovered = true;
        }

        if (enabled && _hovered && mouseState.LeftButton == ButtonState.Pressed)
        {
            _scene.ChangeScene(_nextSceneKey);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Rectangle buttonRect = GetBounds();
        Vector2 textSize = AssetManager.ArialFont.MeasureString(_buttonText);
        Vector2 textPosition = new Vector2(
            buttonRect.X + (buttonRect.Width - textSize.X) / 2f,
            buttonRect.Y + (buttonRect.Height - textSize.Y) / 2f);

        bool enabled = IsEnabled();
        Texture2D pixel = AssetManager.GetTexture(Art.pixel);
        Color fill = !enabled ? new Color(22, 25, 28, 210) : _hovered ? new Color(224, 204, 138, 230) : new Color(34, 39, 45, 220);
        Color border = !enabled ? new Color(80, 84, 88, 210) : _hovered ? new Color(255, 235, 160) : new Color(190, 164, 105, 210);
        Color textColor = !enabled ? new Color(118, 120, 122) : _hovered ? new Color(10, 12, 14) : new Color(242, 238, 220);

        spriteBatch.Draw(pixel, buttonRect, fill);
        spriteBatch.Draw(pixel, new Rectangle(buttonRect.Left, buttonRect.Top, buttonRect.Width, 1), border);
        spriteBatch.Draw(pixel, new Rectangle(buttonRect.Left, buttonRect.Bottom - 1, buttonRect.Width, 1), border * 0.75f);
        spriteBatch.Draw(pixel, new Rectangle(buttonRect.Left, buttonRect.Top, 1, buttonRect.Height), border * 0.75f);
        spriteBatch.Draw(pixel, new Rectangle(buttonRect.Right - 1, buttonRect.Top, 1, buttonRect.Height), border * 0.75f);
        spriteBatch.DrawString(AssetManager.ArialFont, _buttonText, textPosition, textColor);
    }

    private bool IsEnabled()
    {
        return _isEnabled == null || _isEnabled();
    }
}
