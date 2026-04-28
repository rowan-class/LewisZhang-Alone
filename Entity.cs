using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class Entity
{
    protected Texture2D _texture;
    protected Vector2 _position;
    protected Vector2 _velocity;
    protected Point _rectOffset = new Point(6, 6);
    protected Rectangle _rect;
    protected bool _isActive = true;
    protected Scene _scene;

    public Vector2 Position => _position;
    public bool IsActive => _isActive;
    public Scene Scene => _scene;

    public Entity(Texture2D texture, Vector2 position)
    {
        _texture = texture;
        _position = position;
        _velocity = Vector2.Zero;
        UpdateRect();
    }

    public virtual void Update(GameTime gameTime)
    {
        Vector2 velocity = _velocity;
        if (_scene != null)
        {
            velocity = _scene.CheckForGridCollision(this, velocity);
        }
        _position += velocity;
        UpdateRect();
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        if (_isActive && _texture != null)
        {
            spriteBatch.Draw(_texture, _position, Color.White);
        }
    }

    public virtual Rectangle GetBounds()
    {
        if (_texture == null)
        {
            return Rectangle.Empty;
        }

        UpdateRect();
        return _rect;
    }

    public void Deactivate()
    {
        _isActive = false;
    }

    public virtual void RemoveFromScene(Scene scene)
    {
        // Hook for derived entities to clean up when removed.
    }

    internal void SetScene(Scene scene)
    {
        _scene = scene;
    }

    public bool CheckCollision(Entity other)
    {
        return _isActive && other._isActive && GetBounds().Intersects(other.GetBounds());
    }

    public void Translate(Vector2 translation)
    {
        _position.X += translation.X;
        _position.Y += translation.Y;
        UpdateRect();
    }

    protected virtual void UpdateRect()
    {
        if (_texture == null)
        {
            _rect = Rectangle.Empty;
            return;
        }

        _rect = new Rectangle(
            (int)_position.X + _rectOffset.X,
            (int)_position.Y + _rectOffset.Y,
            Grid.TileSize - _rectOffset.X * 2,
            Grid.TileSize - _rectOffset.Y
        );
    }
}
