using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public abstract class SpaceEntity : Entity
{
    public PositionSpace Space { get; private set; }
    public Vector2 LocalPosition { get; private set; }

    protected SpaceEntity(PositionSpace space, Vector2 localPosition, Point size)
        : base(Vector2.Zero, size)
    {
        Space = space;
        LocalPosition = localPosition;
    }

    public override void Update(GameTime gameTime)
    {
        if (_scene is LevelScene levelScene)
        {
            _position = levelScene.ResolvePosition(Space, LocalPosition);
        }
    }

    public void SetSpace(PositionSpace space, Vector2 localPosition)
    {
        Space = space;
        LocalPosition = localPosition;

        if (_scene is LevelScene levelScene)
        {
            _position = levelScene.ResolvePosition(Space, LocalPosition);
        }
    }

    public void SetLocalPosition(Vector2 localPosition)
    {
        SetSpace(Space, localPosition);
    }
}
