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

    public override void Update(GameTime gameTime)
    {
        if (_scene is LevelScene levelScene)
        {
            Rectangle bounds = levelScene.GroundCollisionBounds;
            _position = new Vector2(bounds.X, bounds.Y);
            _size = new Point(bounds.Width, bounds.Height);
            return;
        }

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
    }
}
