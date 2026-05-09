using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class VehicleResources
{
    public const float MaxFuel = 100f;
    public const float InitialFuel = 20f;

    public float Fuel { get; private set; } = InitialFuel;
    public float FuelRatio => System.Math.Clamp(Fuel / MaxFuel, 0f, 1f);
    public bool HasFuel => Fuel > 0f;

    public void AddFuelByRatio(float ratio)
    {
        if (ratio <= 0f)
        {
            return;
        }

        Fuel = System.Math.Clamp(Fuel + MaxFuel * ratio, 0f, MaxFuel);
    }

    public void ConsumeFuel(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        Fuel = System.MathF.Max(0f, Fuel - amount);
    }

    public void SetFuel(float fuel)
    {
        Fuel = System.Math.Clamp(fuel, 0f, MaxFuel);
    }
}

public class VehicleEntity : SpriteEntity
{
    private const float PoweredAcceleration = 100f;
    private const float CoastDeceleration = 80f;
    private const float HandbrakeDeceleration = 240f;
    private const float SolarSpeedBonus = 50f;
    private const float AcceleratingFuelUsePerSecond = 4f;
    private const float CruisingFuelUsePerSecond = 1.5f;
    private const int WheelSize = 104;
    private const float WheelRadius = WheelSize / 2f;
    private const float WheelSuspensionMaxTravel = 58f;
    private const float WheelSuspensionSharpness = 18f;

    private static readonly Vector2[] WheelCentersLocal =
    {
        new(75f, 485f),
        new(248f, 485f),
        new(466f, 485f),
        new(693f, 485f),
        new(895f, 485f)
    };

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
        new Rectangle(90+535, 204, 300, 12),
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

    private readonly VehicleResources _resources = new();
    private readonly float[] _wheelInitialRotations;
    private readonly float[] _wheelSuspensionOffsets = new float[WheelCentersLocal.Length];
    private float _speed;
    private float _wheelRotation;

    public bool Powered { get; private set; }
    public bool HandbrakeActive { get; private set; }
    public bool SolarDriveActive { get; private set; }
    public float BaseSpeed => _speed;
    public float Speed => HandbrakeActive ? 0f : System.MathF.Min(WorldConfig.VehicleMaxSpeed, _speed + GetSolarSpeedBonus());
    public VehicleResources Resources => _resources;
    public float Fuel => _resources.Fuel;
    public float FuelRatio => _resources.FuelRatio;
    public Rectangle CabinBoundsWorld => OffsetRectangle(WorldConfig.VehicleCabinBoundsLocal);

    public VehicleEntity()
        : base(Art.Vehicle, WorldConfig.VehiclePosition, WorldConfig.VehicleSize)
    {
        _wheelInitialRotations = CreateWheelInitialRotations();
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
        else if (Powered && _resources.HasFuel)
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

        _wheelRotation = MathHelper.WrapAngle(_wheelRotation + Speed * dt / WheelRadius);
        UpdateWheelSuspension(dt);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        DrawWheelsLayer(spriteBatch);
        DrawBody(spriteBatch);
    }

    public void DrawBody(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }

    public void DrawWheelsLayer(SpriteBatch spriteBatch)
    {
        if (!_isActive)
        {
            return;
        }

        DrawWheels(spriteBatch, _position, 1f, _wheelRotation, _wheelInitialRotations, Color.White, _wheelSuspensionOffsets);
    }

    public static float GetWheelRotationDelta(float travelDistance, float vehicleScale)
    {
        float renderedWheelRadius = WheelRadius * System.MathF.Max(0.001f, vehicleScale);
        return travelDistance / renderedWheelRadius;
    }

    public static void DrawWheels(
        SpriteBatch spriteBatch,
        Vector2 vehiclePosition,
        float vehicleScale,
        float wheelRotation,
        float[] wheelInitialRotations,
        Color tint,
        float[] suspensionOffsets = null)
    {
        Texture2D wheelTexture = AssetManager.GetTexture(Art.Wheel);
        Vector2 origin = new(wheelTexture.Width / 2f, wheelTexture.Height / 2f);
        float scale = WheelSize * vehicleScale / wheelTexture.Width;
        for (int i = 0; i < WheelCentersLocal.Length; i++)
        {
            float initialRotation = i < wheelInitialRotations.Length ? wheelInitialRotations[i] : 0f;
            float suspensionOffset = suspensionOffsets != null && i < suspensionOffsets.Length ? suspensionOffsets[i] : 0f;
            spriteBatch.Draw(
                wheelTexture,
                vehiclePosition + WheelCentersLocal[i] * vehicleScale - new Vector2(0f, suspensionOffset * vehicleScale),
                null,
                tint,
                wheelRotation + initialRotation,
                origin,
                scale,
                SpriteEffects.None,
                0f);
        }
    }

    public void TogglePower()
    {
        SetPowered(!Powered);
    }

    public void SetPowered(bool powered)
    {
        Powered = !HandbrakeActive && powered && _resources.HasFuel;
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

        _resources.AddFuelByRatio(ratio);
    }

    public VehicleSaveData CaptureSaveData()
    {
        return new VehicleSaveData
        {
            Speed = _speed,
            Fuel = _resources.Fuel,
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
        _resources.SetFuel(data.Fuel);
        HandbrakeActive = data.HandbrakeActive && _speed > 0f;
        SolarDriveActive = Game1.SolarPanelEnabled && Game1.SolarPanelHasEnergy && data.SolarDriveActive && !HandbrakeActive;
        Powered = !HandbrakeActive && data.Powered && _resources.HasFuel;
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
        if (!Game1.SolarPanelEnabled || !Game1.SolarPanelHasEnergy)
        {
            SolarDriveActive = false;
            return;
        }

        SolarDriveActive = !SolarDriveActive;
    }

    public void SetSolarDriveActive(bool isActive)
    {
        SolarDriveActive = Game1.SolarPanelEnabled && Game1.SolarPanelHasEnergy && isActive && !HandbrakeActive;
    }

    private void ConsumeFuel(float amount)
    {
        _resources.ConsumeFuel(amount);

        if (!_resources.HasFuel)
        {
            Powered = false;
        }
    }

    private void UpdateWheelSuspension(float dt)
    {
        float smoothing = dt <= 0f ? 1f : 1f - System.MathF.Exp(-WheelSuspensionSharpness * dt);

        for (int i = 0; i < _wheelSuspensionOffsets.Length; i++)
        {
            float targetOffset = GetWheelSuspensionTarget(i);
            _wheelSuspensionOffsets[i] = MathHelper.Lerp(_wheelSuspensionOffsets[i], targetOffset, smoothing);
        }
    }

    private float GetWheelSuspensionTarget(int wheelIndex)
    {
        if (_scene is not LevelScene levelScene)
        {
            return 0f;
        }

        Vector2 wheelCenter = _position + WheelCentersLocal[wheelIndex];
        float wheelBottom = wheelCenter.Y + WheelRadius;
        float targetOffset = 0f;

        foreach (Rectangle obstacle in levelScene.GetWheelSuspensionObstacleBounds())
        {
            if (obstacle.Top >= wheelBottom)
            {
                continue;
            }

            float obstacleCenterX = obstacle.Center.X;
            float horizontalReach = obstacle.Width / 2f + WheelRadius;
            float distanceX = System.MathF.Abs(wheelCenter.X - obstacleCenterX);
            if (distanceX > horizontalReach)
            {
                continue;
            }

            float contactAmount = MathHelper.Clamp(1f - distanceX / horizontalReach, 0f, 1f);
            float curvedContact = System.MathF.Sin(contactAmount * MathHelper.PiOver2);
            float liftAmount = (wheelBottom - obstacle.Top) * curvedContact;
            targetOffset = System.MathF.Max(targetOffset, liftAmount);
        }

        return MathHelper.Clamp(targetOffset, 0f, WheelSuspensionMaxTravel);
    }

    private float GetSolarSpeedBonus()
    {
        return Game1.SolarPanelEnabled && Game1.SolarPanelHasEnergy && SolarDriveActive ? SolarSpeedBonus : 0f;
    }

    private float GetMaximumBaseSpeed()
    {
        return System.MathF.Max(0f, WorldConfig.VehicleMaxSpeed - GetSolarSpeedBonus());
    }

    public static float[] CreateWheelInitialRotations()
    {
        System.Random random = new(System.Guid.NewGuid().GetHashCode());
        float[] rotations = new float[WheelCentersLocal.Length];
        for (int i = 0; i < rotations.Length; i++)
        {
            rotations[i] = (float)(random.NextDouble() * MathHelper.TwoPi);
        }

        return rotations;
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
