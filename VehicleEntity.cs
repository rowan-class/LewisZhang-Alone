using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class VehicleEntity : SpriteEntity
{
    private const float PoweredAcceleration = 180f;
    private const float CoastDeceleration = 120f;
    private const float MaxSpeed = 260f;

    // Edit these rectangles later to hand-author interior floors, walls, and shelves.
    // They are in vehicle-local coordinates.
    private readonly Rectangle[] _interiorCollisionLocalRectangles =
    {
        new Rectangle(90, 420, 820, 24),
        new Rectangle(610, 280, 220, 20),
        new Rectangle(350, 180, 170, 20),
        new Rectangle(90, 110, 20, 180),
        new Rectangle(910, 110, 20, 320),
    };

    private float _speed;

    public bool Powered { get; private set; }
    public float Speed => _speed;
    public Rectangle CabinBoundsWorld => OffsetRectangle(WorldConfig.VehicleCabinBoundsLocal);

    public VehicleEntity()
        : base(Art.Vehicle, WorldConfig.VehiclePosition, WorldConfig.VehicleSize)
    {
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (Powered)
        {
            _speed = System.MathF.Min(MaxSpeed, _speed + PoweredAcceleration * dt);
        }
        else
        {
            _speed = System.MathF.Max(0f, _speed - CoastDeceleration * dt);
        }
    }

    public void TogglePower()
    {
        Powered = !Powered;
    }

    public void AdjustSpeed(float delta)
    {
        _speed = System.Math.Clamp(_speed + delta, 0f, MaxSpeed);
    }

    public bool IsInsideCabin(Rectangle playerBounds)
    {
        return CabinBoundsWorld.Contains(playerBounds.Center);
    }

    public IEnumerable<Rectangle> GetInteriorCollisionWorldRectangles()
    {
        foreach (Rectangle localRect in _interiorCollisionLocalRectangles)
        {
            yield return OffsetRectangle(localRect);
        }
    }

    public override IEnumerable<Rectangle> GetDebugRectangles()
    {
        foreach (Rectangle rect in base.GetDebugRectangles())
        {
            yield return rect;
        }

        foreach (Rectangle rect in GetInteriorCollisionWorldRectangles())
        {
            yield return rect;
        }
    }

    private Rectangle OffsetRectangle(Rectangle localRect)
    {
        return new Rectangle(
            (int)_position.X + localRect.X,
            (int)_position.Y + localRect.Y,
            localRect.Width,
            localRect.Height);
    }
}
