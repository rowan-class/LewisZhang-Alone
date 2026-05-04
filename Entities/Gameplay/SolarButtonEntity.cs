using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class SolarButtonEntity : SpaceEntity
{
    private const int TriggerHeight = 12;
    private const int TriggerInsetX = 6;

    private readonly VehicleEntity _vehicle;
    private bool _isHeadContactActive;
    private bool _wasHeadContactLastFrame;

    public SolarButtonEntity(VehicleEntity vehicle)
        : base(PositionSpace.Vehicle, WorldConfig.SolarButtonTopLeftLocal, WorldConfig.FuelButtonSize)
    {
        _vehicle = vehicle;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!Game1.SolarPanelEnabled || _scene is not LevelScene levelScene)
        {
            _vehicle.SetSolarDriveActive(false);
            _isHeadContactActive = false;
            _wasHeadContactLastFrame = false;
            return;
        }

        _isHeadContactActive = IsPressedBy(levelScene.Player);

        if (_isHeadContactActive && !_wasHeadContactLastFrame)
        {
            _vehicle.ToggleSolarDrive();
        }

        _wasHeadContactLastFrame = _isHeadContactActive;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!Game1.SolarPanelEnabled)
        {
            return;
        }

        Art art = _vehicle.SolarDriveActive ? Art.FuelButtonPressed : Art.FuelButtonIdle;
        spriteBatch.Draw(AssetManager.GetTexture(art), GetBounds(), Color.White);
    }

    public override IEnumerable<Rectangle> GetDebugRectangles()
    {
        if (!Game1.SolarPanelEnabled)
        {
            yield break;
        }

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
