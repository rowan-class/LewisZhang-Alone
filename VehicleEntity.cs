using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class VehicleEntity : SpriteEntity
{
    private const float PoweredAcceleration = 100f;
    private const float CoastDeceleration = 120f;
    private const float MaxFuel = 100f;
    private const float AcceleratingFuelUsePerSecond = 4f;
    private const float CruisingFuelUsePerSecond = 1.5f;

    // Edit these rectangles later to hand-author interior floors, walls, and shelves.
    // They are in vehicle-local coordinates.
    private readonly Rectangle[] _interiorCollisionLocalRectangles =
    {
        // 一层：
        new Rectangle(24, 422, 900, 16),
        // 二层：
        new Rectangle(90, 326, 320, 12),
        new Rectangle(90+420, 326, 330, 12),
        // 三层：
        new Rectangle(90, 204, 320, 12),
        new Rectangle(90+520, 204, 320, 12),
        // 车顶：
        new Rectangle(90, 68, 820, 16),
        // 左墙壁：
        new Rectangle(90, 110, 20, 180),
        // 右墙壁：
        new Rectangle(910, 110, 20, 320),
    };

    private float _speed;
    private float _fuel = MaxFuel;

    public bool Powered { get; private set; }
    public float Speed => _speed;
    public float Fuel => _fuel;
    public float FuelRatio => System.Math.Clamp(_fuel / MaxFuel, 0f, 1f);
    public Rectangle CabinBoundsWorld => OffsetRectangle(WorldConfig.VehicleCabinBoundsLocal);

    public VehicleEntity()
        : base(Art.Vehicle, WorldConfig.VehiclePosition, WorldConfig.VehicleSize)
    {
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (Powered && _fuel > 0f)
        {
            bool isCruising = _speed >= WorldConfig.VehicleMaxSpeed;
            _speed = System.MathF.Min(WorldConfig.VehicleMaxSpeed, _speed + PoweredAcceleration * dt);
            ConsumeFuel((isCruising ? CruisingFuelUsePerSecond : AcceleratingFuelUsePerSecond) * dt);
        }
        else
        {
            Powered = false;
            _speed = System.MathF.Max(0f, _speed - CoastDeceleration * dt);
        }
    }

    public void TogglePower()
    {
        SetPowered(!Powered);
    }

    public void SetPowered(bool powered)
    {
        Powered = powered && _fuel > 0f;
    }

    public void AdjustSpeed(float delta)
    {
        _speed = System.Math.Clamp(_speed + delta, 0f, WorldConfig.VehicleMaxSpeed);
    }

    private void ConsumeFuel(float amount)
    {
        _fuel = System.MathF.Max(0f, _fuel - amount);

        if (_fuel <= 0f)
        {
            Powered = false;
        }
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
