using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class VehicleEntity : SpriteEntity
{
    private const float PoweredAcceleration = 100f;
    private const float CoastDeceleration = 80f;
    private const float HandbrakeDeceleration = 240f;
    private const float SolarSpeedBonus = 50f;
    private const float MaxFuel = 100f;
    private const float InitialFuel = 20f;
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
        new Rectangle(90+420, 326, 400, 12),
        // 三层：
        new Rectangle(90, 204, 320, 12),
        new Rectangle(90+520, 204, 320, 12),
        // 车顶：
        new Rectangle(60, 64, 820, 20),
        // new Rectangle(60+360, 100, 120, 12),
        // new Rectangle(90, 68, 820, 16),
        // 左墙壁：
        new Rectangle(130, 110, 20, 230),
        new Rectangle(40, 95, 20, 225),
        // 右墙壁：
        new Rectangle(890, 110, 20, 320),
        new Rectangle(910, 110, 20, 320),
    };

    private float _speed;
    private float _fuel = InitialFuel;

    public bool Powered { get; private set; }
    public bool HandbrakeActive { get; private set; }
    public bool SolarDriveActive { get; private set; }
    public float BaseSpeed => _speed;
    public float Speed => HandbrakeActive ? 0f : System.MathF.Min(WorldConfig.VehicleMaxSpeed, _speed + GetSolarSpeedBonus());
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

        if (HandbrakeActive)
        {
            Powered = false;
            _speed = System.MathF.Max(0f, _speed - HandbrakeDeceleration * dt);
            if (_speed <= 0f)
            {
                _speed = 0f;
                HandbrakeActive = false;
            }
        }
        else if (Powered && _fuel > 0f)
        {
            float maximumBaseSpeed = GetMaximumBaseSpeed();
            bool isCruising = _speed >= maximumBaseSpeed;
            _speed = System.MathF.Min(maximumBaseSpeed, _speed + PoweredAcceleration * dt);
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
        Powered = !HandbrakeActive && powered && _fuel > 0f;
    }

    public void AdjustSpeed(float delta)
    {
        _speed = System.Math.Clamp(_speed + delta, 0f, GetMaximumBaseSpeed());
        if (_speed <= 0f)
        {
            HandbrakeActive = false;
        }
    }

    public void SetBaseSpeed(float speed)
    {
        _speed = System.Math.Clamp(speed, 0f, GetMaximumBaseSpeed());
        if (_speed <= 0f)
        {
            HandbrakeActive = false;
        }
    }

    public void MoveBaseSpeedTowards(float targetSpeed, float acceleration, float dt)
    {
        float maxDelta = System.MathF.Max(0f, acceleration) * System.MathF.Max(0f, dt);
        float delta = targetSpeed - _speed;
        if (System.MathF.Abs(delta) <= maxDelta)
        {
            SetBaseSpeed(targetSpeed);
            return;
        }

        SetBaseSpeed(_speed + System.MathF.Sign(delta) * maxDelta);
    }

    public void AddFuelByRatio(float ratio)
    {
        if (ratio <= 0f)
        {
            return;
        }

        _fuel = System.Math.Clamp(_fuel + MaxFuel * ratio, 0f, MaxFuel);
    }

    public VehicleSaveData CaptureSaveData()
    {
        return new VehicleSaveData
        {
            Speed = _speed,
            Fuel = _fuel,
            Powered = Powered,
            HandbrakeActive = HandbrakeActive,
            SolarDriveActive = SolarDriveActive
        };
    }

    public void RestoreSaveData(VehicleSaveData data)
    {
        if (data == null)
        {
            return;
        }

        _speed = System.Math.Clamp(data.Speed, 0f, WorldConfig.VehicleMaxSpeed);
        _fuel = System.Math.Clamp(data.Fuel, 0f, MaxFuel);
        HandbrakeActive = data.HandbrakeActive && _speed > 0f;
        SolarDriveActive = Game1.SolarPanelEnabled && data.SolarDriveActive && !HandbrakeActive;
        Powered = !HandbrakeActive && data.Powered && _fuel > 0f;
    }

    public bool TryActivateHandbrake()
    {
        if (HandbrakeActive || Speed <= 0f)
        {
            return false;
        }

        HandbrakeActive = true;
        Powered = false;
        SolarDriveActive = false;
        return true;
    }

    public void ReleaseHandbrake()
    {
        HandbrakeActive = false;
    }

    public void ToggleSolarDrive()
    {
        if (!Game1.SolarPanelEnabled)
        {
            SolarDriveActive = false;
            return;
        }

        SolarDriveActive = !SolarDriveActive;
    }

    public void SetSolarDriveActive(bool isActive)
    {
        SolarDriveActive = Game1.SolarPanelEnabled && isActive && !HandbrakeActive;
    }

    private void ConsumeFuel(float amount)
    {
        _fuel = System.MathF.Max(0f, _fuel - amount);

        if (_fuel <= 0f)
        {
            Powered = false;
        }
    }

    private float GetSolarSpeedBonus()
    {
        return Game1.SolarPanelEnabled && SolarDriveActive ? SolarSpeedBonus : 0f;
    }

    private float GetMaximumBaseSpeed()
    {
        return System.MathF.Max(0f, WorldConfig.VehicleMaxSpeed - GetSolarSpeedBonus());
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
