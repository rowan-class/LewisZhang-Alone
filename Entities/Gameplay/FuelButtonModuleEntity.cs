using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class FuelButtonModuleEntity : SpaceEntity
{
    private const float RefuelRatio = 0.3f;
    private const int TriggerHeight = 12;
    private const int TriggerInsetX = 6;

    private readonly VehicleEntity _vehicle;
    private readonly FuelPortEntity _fuelPort;
    private bool _isHeadContactActive;
    private bool _wasHeadContactLastFrame;
    private float _pressedVisualTimer;
    private float _cooldownTimer;

    public FuelButtonModuleEntity(VehicleEntity vehicle, FuelPortEntity fuelPort)
        : base(PositionSpace.Vehicle, WorldConfig.FuelButtonTopLeftLocal, WorldConfig.FuelButtonSize)
    {
        _vehicle = vehicle;
        _fuelPort = fuelPort;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _pressedVisualTimer = System.MathF.Max(0f, _pressedVisualTimer - dt);
        _cooldownTimer = System.MathF.Max(0f, _cooldownTimer - dt);

        if (_scene is not LevelScene levelScene)
        {
            _isHeadContactActive = false;
            _wasHeadContactLastFrame = false;
            _pressedVisualTimer = 0f;
            _cooldownTimer = 0f;
            return;
        }

        _isHeadContactActive = IsPressedBy(levelScene.Player);

        bool canActivate = _cooldownTimer <= 0f;
        if (_isHeadContactActive && !_wasHeadContactLastFrame && canActivate && _fuelPort.TryConsumeCharge())
        {
            _vehicle.AddFuelByRatio(RefuelRatio);
            _pressedVisualTimer = WorldConfig.FuelButtonPressedDuration;
            _cooldownTimer = WorldConfig.FuelButtonCooldownDuration;
        }

        _wasHeadContactLastFrame = _isHeadContactActive;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Art art = _pressedVisualTimer > 0f ? Art.FuelButtonPressed : Art.FuelButtonIdle;
        spriteBatch.Draw(AssetManager.GetTexture(art), GetBounds(), Color.White);
    }

    public override IEnumerable<Rectangle> GetDebugRectangles()
    {
        foreach (Rectangle rect in base.GetDebugRectangles())
        {
            yield return rect;
        }

        yield return GetHeadHitBounds();
    }

    private bool IsPressedBy(Player player)
    {
        Rectangle headRect = GetPlayerHeadBounds(player);
        return player.Velocity.Y < 0f
            && headRect != Rectangle.Empty
            && headRect.Intersects(GetHeadHitBounds());
    }

    private Rectangle GetPlayerHeadBounds(Player player)
    {
        Rectangle playerBounds = player.GetBounds();
        if (playerBounds == Rectangle.Empty)
        {
            return Rectangle.Empty;
        }

        int width = System.Math.Max(1, playerBounds.Width - 16);
        return new Rectangle(
            playerBounds.X + 8,
            playerBounds.Y,
            width,
            System.Math.Max(1, playerBounds.Height / 4));
    }

    private Rectangle GetHeadHitBounds()
    {
        Rectangle bounds = GetBounds();
        if (bounds == Rectangle.Empty)
        {
            return Rectangle.Empty;
        }

        return new Rectangle(
            bounds.X + TriggerInsetX,
            bounds.Bottom,
            System.Math.Max(1, bounds.Width - TriggerInsetX * 2),
            TriggerHeight);
    }
}
