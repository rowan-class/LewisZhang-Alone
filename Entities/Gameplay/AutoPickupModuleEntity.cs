using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class AutoPickupModuleEntity : SpaceEntity
{
    private static readonly Color ModuleColor = new(180, 80, 255);

    public bool IsEnabled { get; private set; }

    public AutoPickupModuleEntity()
        : base(PositionSpace.Vehicle, WorldConfig.AutoPickupModuleTopLeftLocal, WorldConfig.AutoPickupModuleSize)
    {
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!IsEnabled || _scene is not LevelScene levelScene)
        {
            return;
        }

        Rectangle suctionBounds = GetSuctionBounds();

        foreach (FuelBarrelEntity barrel in levelScene.GetFuelBarrels())
        {
            if (!CanAutoPickup(barrel) || !suctionBounds.Contains(barrel.GetBounds()))
            {
                continue;
            }

            barrel.Drop(PositionSpace.Vehicle, WorldConfig.AutoPickupStoredBarrelTopLeftLocal);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsEnabled)
        {
            return;
        }

        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), GetBounds(), ModuleColor);
    }

    public void ToggleEnabled()
    {
        IsEnabled = !IsEnabled;
    }

    public void SetEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }

    public override IEnumerable<Rectangle> GetDebugRectangles()
    {
        if (!IsEnabled)
        {
            yield break;
        }

        foreach (Rectangle rect in base.GetDebugRectangles())
        {
            yield return rect;
        }

        yield return GetSuctionBounds();
    }

    private bool CanAutoPickup(FuelBarrelEntity barrel)
    {
        return barrel.IsActive
            && !barrel.IsCarried
            && barrel.Space == PositionSpace.World;
    }

    private Rectangle GetSuctionBounds()
    {
        Rectangle bounds = GetBounds();
        if (bounds == Rectangle.Empty)
        {
            return Rectangle.Empty;
        }

        int height = System.Math.Max(0, WorldConfig.FakeGroundLocalRect.Y - bounds.Bottom);
        return new Rectangle(bounds.X, bounds.Bottom, bounds.Width, height);
    }
}
