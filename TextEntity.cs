using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class TextEntity : Entity
{
    protected string _text;

    public TextEntity(Vector2 position, string text) : base(null, position)
    {
        _text = text;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.DrawString(AssetManager.ArialFont, _text, _position, Color.White);
    }

    public void SetText(string new_text)
    {
        _text = new_text;
    }
}