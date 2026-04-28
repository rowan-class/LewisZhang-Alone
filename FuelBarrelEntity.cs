using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class FuelBarrelEntity : SpaceEntity
{
    private static readonly Color FuelBarrelColor = Color.LimeGreen;
    private const float Gravity = 1800f;
    private const float MaxFallSpeed = 1000f;

    private Player _carrier;
    private float _verticalVelocity;

    public bool IsCarried => _carrier != null;

    public FuelBarrelEntity(PositionSpace space, Vector2 localPosition)
        : base(space, localPosition, WorldConfig.FuelBarrelSize)
    {
    }

    public override void Update(GameTime gameTime)
    {
        if (_carrier != null)
        {
            _position = _carrier.GetCarryAnchor(_size);
            return;
        }

        base.Update(gameTime);

        if (_scene is not LevelScene levelScene)
        {
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _verticalVelocity = System.MathF.Min(MaxFallSpeed, _verticalVelocity + Gravity * dt);

        _position.Y += _verticalVelocity * dt;
        Rectangle bounds = GetBounds();

        foreach (Rectangle solid in levelScene.GetSolidRectangles())
        {
            if (solid == Rectangle.Empty || !bounds.Intersects(solid))
            {
                continue;
            }

            if (_verticalVelocity >= 0f)
            {
                _position.Y = solid.Top - bounds.Height;
            }
            else
            {
                _position.Y = solid.Bottom;
            }

            _verticalVelocity = 0f;
            break;
        }

        SetLocalPosition(levelScene.WorldToLocal(Space, _position));
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), GetBounds(), FuelBarrelColor);
    }

    public void PickUp(Player player)
    {
        _carrier = player;
        _verticalVelocity = 0f;
    }

    public void Drop(PositionSpace newSpace, Vector2 newLocalPosition)
    {
        _carrier = null;
        _verticalVelocity = 0f;
        SetSpace(newSpace, newLocalPosition);
    }
}
