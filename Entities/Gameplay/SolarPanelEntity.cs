using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class SolarPanelEntity : SpaceEntity
{
    private const float DamageBlinkInterval = 0.18f;
    private float _damageBlinkTimer;

    public bool IsDamaged { get; private set; }

    public SolarPanelEntity()
        : base(PositionSpace.Vehicle, WorldConfig.SolarPanelTopLeftLocal, WorldConfig.SolarPanelSize)
    {
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _damageBlinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!Game1.SolarPanelEnabled)
        {
            return;
        }

        Art art = Game1.SolarPanelHasEnergy && !IsDamaged ? Art.SolarPanelPowered : Art.SolarPanelUnpowered;
        spriteBatch.Draw(AssetManager.GetTexture(art), GetBounds(), GetDamageTint());
    }

    public void Damage()
    {
        IsDamaged = true;
    }

    public void Repair()
    {
        IsDamaged = false;
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
    }

    private Color GetDamageTint()
    {
        if (!IsDamaged)
        {
            return Color.White;
        }

        int frame = (int)(_damageBlinkTimer / DamageBlinkInterval);
        return frame % 2 == 0 ? Color.Red : Color.White;
    }
}
