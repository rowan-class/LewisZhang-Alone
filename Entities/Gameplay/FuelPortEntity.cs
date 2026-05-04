using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class FuelPortEntity : SpaceEntity
{
    private const int InteractionPaddingX = 36;
    private const int InteractionPaddingY = 10;

    private bool _isOpen;

    public bool IsLit { get; private set; }

    public FuelPortEntity()
        : base(PositionSpace.Vehicle, WorldConfig.FuelPortTopLeftLocal, WorldConfig.FuelPortSize)
    {
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_scene is not LevelScene levelScene)
        {
            _isOpen = false;
            return;
        }

        _isOpen = !IsLit
            && levelScene.IsPlayerCarryingFuelBarrel
            && IsPlayerInRange(levelScene.PlayerBounds);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(AssetManager.GetTexture(GetCurrentArt()), GetBounds(), Color.White);
    }

    public bool TryInsertBarrel(Player player, FuelBarrelEntity barrel)
    {
        if (player == null || barrel == null || IsLit || !IsPlayerInRange(player.GetBounds()))
        {
            return false;
        }

        IsLit = true;
        _isOpen = false;
        return true;
    }

    public bool TryConsumeCharge()
    {
        if (!IsLit)
        {
            return false;
        }

        IsLit = false;
        return true;
    }

    public void SetLit(bool isLit)
    {
        IsLit = isLit;
        if (isLit)
        {
            _isOpen = false;
        }
    }

    public override IEnumerable<Rectangle> GetDebugRectangles()
    {
        foreach (Rectangle rect in base.GetDebugRectangles())
        {
            yield return rect;
        }

        yield return GetInteractionBounds();
    }

    private bool IsPlayerInRange(Rectangle playerBounds)
    {
        return GetInteractionBounds().Intersects(playerBounds);
    }

    private Rectangle GetInteractionBounds()
    {
        Rectangle bounds = GetBounds();
        if (bounds == Rectangle.Empty)
        {
            return Rectangle.Empty;
        }

        return new Rectangle(
            bounds.X - InteractionPaddingX,
            bounds.Y - InteractionPaddingY,
            bounds.Width + InteractionPaddingX * 2,
            bounds.Height + InteractionPaddingY * 2);
    }

    private Art GetCurrentArt()
    {
        if (IsLit)
        {
            return Art.FuelPortLit;
        }

        return _isOpen ? Art.FuelPortOpen : Art.FuelPortClosed;
    }
}
