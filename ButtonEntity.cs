using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LewisZhang_Alone;

public class ButtonEntity : SpriteEntity
{
    private readonly string _buttonText;
    private readonly string _nextSceneKey;
    private bool _hovered;

    public ButtonEntity(Art art, Vector2 position)
        : this(art, position, "Start", "level1")
    {
    }

    public ButtonEntity(Art art, Vector2 position, string buttonText, string nextSceneKey)
        : base(art, position)
    {
        _buttonText = buttonText;
        _nextSceneKey = nextSceneKey;

        Vector2 stringDisplaySize = AssetManager.ArialFont.MeasureString(_buttonText);
        _size = new Point(
            System.Math.Max(200, (int)stringDisplaySize.X + 20),
            System.Math.Max(60, (int)stringDisplaySize.Y + 20));
    }

    public override void Update(GameTime gameTime)
    {
        _hovered = false;

        MouseState mouseState = Mouse.GetState();
        Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
        if (GetBounds().Contains(mousePosition))
        {
            _hovered = true;
        }

        if (_hovered && mouseState.LeftButton == ButtonState.Pressed)
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

        Color buttonColor = _hovered ? Color.LightGray : Color.White;
        spriteBatch.Draw(AssetManager.GetTexture(Art.Button), buttonRect, buttonColor);
        spriteBatch.DrawString(AssetManager.ArialFont, _buttonText, textPosition, Color.Black);
    }
}
