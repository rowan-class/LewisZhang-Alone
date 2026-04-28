using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class GroundEntity : SpaceEntity
{
    private static readonly Color GroundColor = new(60, 60, 60);

    public GroundEntity()
        : base(
            PositionSpace.World,
            new Vector2(WorldConfig.FakeGroundLocalRect.X, WorldConfig.FakeGroundLocalRect.Y),
            new Point(WorldConfig.FakeGroundLocalRect.Width, WorldConfig.FakeGroundLocalRect.Height))
    {
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), GetBounds(), GroundColor);
    }
}
