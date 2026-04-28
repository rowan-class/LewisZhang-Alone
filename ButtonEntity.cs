using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LewisZhang_Alone;

public class ButtonEntity : SpriteEntity
{
    private readonly string _buttonText;
    private readonly string _nextSceneKey;
    private bool hovered;

    public ButtonEntity(Art art, Vector2 position) : this(art, position, "Start", "level1")
    {
    }

    public ButtonEntity(Art art, Vector2 position, string buttonText, string nextSceneKey) : base(art, position)
    {
        _buttonText = buttonText;
        _nextSceneKey = nextSceneKey;

        Vector2 stringDisplaySize = AssetManager.ArialFont.MeasureString(_buttonText);
        _rect.Width = Math.Max(200, (int)stringDisplaySize.X + 20);
        _rect.Height = Math.Max(60, (int)stringDisplaySize.Y + 20);
        _rect.X = (int)position.X;
        _rect.Y = (int)position.Y;
    }

    public override void Update(GameTime gameTime)
    {
        hovered = false;
        _rect.X = (int)_position.X;
        _rect.Y = (int)_position.Y;
        MouseState mouseState = Mouse.GetState();
        Vector2 mouse_pos = new Vector2(mouseState.X, mouseState.Y);
        if (_rect.Contains(mouse_pos))
        {
            hovered = true;
        }
        if (hovered)
        {
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                // Debug.WriteLine("Button Clicked!");
                _scene.ChangeScene(_nextSceneKey);
            }
        }

    }

    public override Rectangle GetBounds()
    {
        _rect.X = (int)_position.X;
        _rect.Y = (int)_position.Y;
        return _rect;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Color button_color = hovered ? Color.LightGray : Color.White;
        Rectangle buttonRect = new Rectangle((int)_position.X, (int)_position.Y, _rect.Width, _rect.Height);
        Vector2 textSize = AssetManager.ArialFont.MeasureString(_buttonText);
        Vector2 textPosition = new Vector2(
            buttonRect.X + (buttonRect.Width - textSize.X) / 2f,
            buttonRect.Y + (buttonRect.Height - textSize.Y) / 2f);

        spriteBatch.Draw(AssetManager.GetTexture(Art.Button), buttonRect, button_color);
        spriteBatch.DrawString(AssetManager.ArialFont, _buttonText, textPosition, Color.Black);
    }
}
