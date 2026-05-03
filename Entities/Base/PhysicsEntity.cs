using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public abstract class PhysicsEntity : SpriteEntity
{
    protected Vector2 velocity = Vector2.Zero;
    protected bool isGrounded;
    protected float gravity = 1800f;
    protected float maxFallSpeed = 1000f;

    public bool IsGrounded => isGrounded;
    public Vector2 Velocity => velocity;

    protected PhysicsEntity(Art art, Vector2 position, Point size)
        : base(art, position, size)
    {
    }

    protected abstract IEnumerable<Rectangle> GetSolidRectangles();

    protected void SimulatePhysics(float dt)
    {
        isGrounded = false;
        velocity.Y = System.MathF.Min(maxFallSpeed, velocity.Y + gravity * dt);

        List<Rectangle> solidRectangles = new(GetSolidRectangles());
        MoveHorizontally(velocity.X * dt, solidRectangles);
        MoveVertically(velocity.Y * dt, solidRectangles);

        if (velocity.Y >= 0f && IsStandingOnSolid(solidRectangles))
        {
            isGrounded = true;
            velocity.Y = 0f;
        }
    }

    private void MoveHorizontally(float deltaX, List<Rectangle> solidRectangles)
    {
        _position.X += deltaX;
        Rectangle bounds = GetBounds();

        foreach (Rectangle solid in solidRectangles)
        {
            if (solid == Rectangle.Empty || !bounds.Intersects(solid))
            {
                continue;
            }

            if (deltaX > 0f)
            {
                _position.X = solid.Left - bounds.Width;
            }
            else if (deltaX < 0f)
            {
                _position.X = solid.Right;
            }

            velocity.X = 0f;
            bounds = GetBounds();
        }
    }

    private void MoveVertically(float deltaY, List<Rectangle> solidRectangles)
    {
        _position.Y += deltaY;
        Rectangle bounds = GetBounds();

        foreach (Rectangle solid in solidRectangles)
        {
            if (solid == Rectangle.Empty || !bounds.Intersects(solid))
            {
                continue;
            }

            if (deltaY > 0f)
            {
                _position.Y = solid.Top - bounds.Height;
                isGrounded = true;
            }
            else if (deltaY < 0f)
            {
                _position.Y = solid.Bottom;
            }

            velocity.Y = 0f;
            bounds = GetBounds();
        }
    }

    private bool IsStandingOnSolid(List<Rectangle> solidRectangles)
    {
        Rectangle bounds = GetBounds();
        if (bounds == Rectangle.Empty)
        {
            return false;
        }

        int inset = System.Math.Min(4, System.Math.Max(1, bounds.Width / 8));
        Rectangle groundProbe = new Rectangle(
            bounds.Left + inset,
            bounds.Bottom,
            System.Math.Max(1, bounds.Width - inset * 2),
            2);

        foreach (Rectangle solid in solidRectangles)
        {
            if (solid != Rectangle.Empty && groundProbe.Intersects(solid))
            {
                return true;
            }
        }

        return false;
    }
}
