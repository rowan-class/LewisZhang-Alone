using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class SolarButtonEntity : SpaceEntity
{
    private const int TriggerHeight = 12;
    private const int TriggerInsetX = 6;
    private const float DamageBlinkInterval = 0.18f;

    private readonly VehicleEntity _vehicle;
    private readonly SolarPanelEntity _solarPanel;
    private bool _isHeadContactActive;
    private bool _wasHeadContactLastFrame;
    private float _damageBlinkTimer;

    public SolarButtonEntity(VehicleEntity vehicle, SolarPanelEntity solarPanel)
        : base(PositionSpace.Vehicle, WorldConfig.SolarButtonTopLeftLocal, WorldConfig.FuelButtonSize)
    {
        _vehicle = vehicle;
        _solarPanel = solarPanel;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _damageBlinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (!Game1.SolarPanelEnabled || !Game1.SolarPanelHasEnergy || _solarPanel.IsDamaged || _scene is not LevelScene levelScene)
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

        Art art = Game1.SolarPanelHasEnergy && !_solarPanel.IsDamaged && _vehicle.SolarDriveActive ? Art.FuelButtonPressed : Art.FuelButtonIdle;
        Color tint = GetTint();
        spriteBatch.Draw(AssetManager.GetTexture(art), GetBounds(), tint);
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

        if (Game1.SolarPanelHasEnergy && !_solarPanel.IsDamaged)
        {
            yield return GetHeadHitBounds();
        }
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

    private Color GetTint()
    {
        if (_solarPanel.IsDamaged)
        {
            int frame = (int)(_damageBlinkTimer / DamageBlinkInterval);
            return frame % 2 == 0 ? Color.Red : Color.White;
        }

        return Game1.SolarPanelHasEnergy ? Color.White : Color.Gray;
    }
}
