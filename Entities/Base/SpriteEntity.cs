using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class SpriteEntity : Entity
{
    protected Texture2D _texture;
    protected Art _art;
    protected Color _tint = Color.White;

    public SpriteEntity(Art art, Vector2 position, Point? size = null)
        : base(position, size ?? Point.Zero)
    {
        _art = art;
        _texture = AssetManager.GetTexture(art);

        if (_size == Point.Zero && _texture != null)
        {
            _size = new Point(_texture.Width, _texture.Height);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!_isActive || _texture == null)
        {
            return;
        }

        spriteBatch.Draw(_texture, GetBounds(), _tint);
    }
}
