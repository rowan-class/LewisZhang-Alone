using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class SolarPanelEntity : SpaceEntity
{
    private static readonly Color SolarColor = Color.Blue;

    public SolarPanelEntity()
        : base(PositionSpace.Vehicle, WorldConfig.SolarPanelTopLeftLocal, WorldConfig.SolarPanelSize)
    {
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!Game1.SolarPanelEnabled)
        {
            return;
        }

        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), GetBounds(), SolarColor);
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
}
