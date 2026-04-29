using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class GroundEntity : SpaceEntity
{
    public GroundEntity()
        : base(
            PositionSpace.World,
            new Vector2(WorldConfig.FakeGroundLocalRect.X, WorldConfig.FakeGroundLocalRect.Y),
            new Point(WorldConfig.FakeGroundLocalRect.Width, WorldConfig.FakeGroundLocalRect.Height))
    {
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
    }
}
