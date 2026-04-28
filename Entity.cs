using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class Entity
{
    protected Vector2 _position;
    protected Point _size;
    protected bool _isActive = true;
    protected Scene _scene;

    public Vector2 Position => _position;
    public Point Size => _size;
    public bool IsActive => _isActive;
    public Scene Scene => _scene;

    public Entity(Vector2 position, Point size)
    {
        _position = position;
        _size = size;
    }

    public virtual void Update(GameTime gameTime)
    {
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
    }

    public virtual Rectangle GetBounds()
    {
        if (_size.X <= 0 || _size.Y <= 0)
        {
            return Rectangle.Empty;
        }

        return new Rectangle(
            (int)System.MathF.Round(_position.X),
            (int)System.MathF.Round(_position.Y),
            _size.X,
            _size.Y);
    }

    public virtual IEnumerable<Rectangle> GetDebugRectangles()
    {
        Rectangle bounds = GetBounds();
        if (bounds != Rectangle.Empty)
        {
            yield return bounds;
        }
    }

    public void Deactivate()
    {
        _isActive = false;
    }

    public virtual void RemoveFromScene(Scene scene)
    {
    }

    internal void SetScene(Scene scene)
    {
        _scene = scene;
    }

    public bool CheckCollision(Entity other)
    {
        return _isActive && other._isActive && GetBounds().Intersects(other.GetBounds());
    }

    public void SetPosition(Vector2 position)
    {
        _position = position;
    }
}
