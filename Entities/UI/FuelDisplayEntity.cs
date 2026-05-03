using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class FuelDisplayEntity : SpaceEntity
{
    private static readonly Color FuelColor = Color.Blue;
    private readonly VehicleEntity _vehicle;

    public FuelDisplayEntity(VehicleEntity vehicle)
        : base(
            PositionSpace.Vehicle,
            new Vector2(
                WorldConfig.FuelDisplayBottomLeftLocal.X,
                WorldConfig.FuelDisplayBottomLeftLocal.Y - WorldConfig.FuelDisplaySize.Y),
            WorldConfig.FuelDisplaySize)
    {
        _vehicle = vehicle;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Rectangle bounds = GetBounds();
        int filledHeight = (int)System.MathF.Round(bounds.Height * _vehicle.FuelRatio);

        if (filledHeight <= 0)
        {
            return;
        }

        Rectangle filledRect = new(
            bounds.X,
            bounds.Bottom - filledHeight,
            bounds.Width,
            filledHeight);

        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), filledRect, FuelColor);
    }
}
