using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class FuelBarrelEntity : SpaceEntity
{
    private const float Gravity = 1800f;
    private const float MaxFallSpeed = 1000f;

    private Player _carrier;
    private float _verticalVelocity;
    private int _orientationQuarterTurns;

    public bool IsCarried => _carrier != null;
    public int OrientationQuarterTurns => _orientationQuarterTurns;

    public FuelBarrelEntity(PositionSpace space, Vector2 localPosition, int orientationQuarterTurns = 0)
        : base(space, localPosition, WorldConfig.FuelBarrelSize)
    {
        _orientationQuarterTurns = NormalizeOrientation(orientationQuarterTurns);
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
        Rectangle bounds = GetBounds();
        Texture2D texture = AssetManager.GetTexture(Art.FuelBarrel);
        Vector2 scale = new(_size.X / (float)texture.Width, _size.Y / (float)texture.Height);

        spriteBatch.Draw(
            texture,
            bounds.Center.ToVector2(),
            null,
            Color.White,
            MathHelper.PiOver2 * _orientationQuarterTurns,
            new Vector2(texture.Width / 2f, texture.Height / 2f),
            scale,
            SpriteEffects.None,
            0f);
    }

    public void PickUp(Player player)
    {
        _carrier = player;
        _verticalVelocity = 0f;
        _orientationQuarterTurns = 0;
    }

    public void Drop(PositionSpace newSpace, Vector2 newLocalPosition)
    {
        _carrier = null;
        _verticalVelocity = 0f;
        _orientationQuarterTurns = 0;
        SetSpace(newSpace, newLocalPosition);
    }

    private static int NormalizeOrientation(int orientationQuarterTurns)
    {
        int normalized = orientationQuarterTurns % 4;
        return normalized < 0 ? normalized + 4 : normalized;
    }
}
