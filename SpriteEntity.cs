using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class SpriteEntity : Entity
{
    protected new Rectangle _rect;
    protected Art _art;

    public SpriteEntity(Art art, Vector2 position) : base(AssetManager.GetTexture(art), position)
    {
        _art = art;
        if (_texture != null)
        {
            _rect = new Rectangle((int)position.X, (int)position.Y, _texture.Width, _texture.Height);
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _rect.X = (int)_position.X;
        _rect.Y = (int)_position.Y;
    }
}
