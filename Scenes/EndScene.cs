using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class EndScene : Scene
{
    private const int StarCount = 96;
    private const float StarBaseSpeed = 70f;
    private const float StarSpeedRange = 210f;
    private const int CapsuleWidth = 248;
    private const int CapsuleHeight = 248;

    private readonly Star[] _stars = new Star[StarCount];
    private float _starScroll;

    public EndScene()
    {
        for (int i = 0; i < _stars.Length; i++)
        {
            int seed = i * 7919 + 37;
            int sideWidth = 360;
            int sideInset = 44;
            int sideX = PositiveModulo(seed * 53, sideWidth);
            int x = i % 2 == 0
                ? sideInset + sideX
                : WorldConfig.ScreenWidth - sideInset - sideWidth + sideX;

            _stars[i] = new Star(
                X: x,
                Y: PositiveModulo(seed * 97, WorldConfig.ScreenHeight),
                Layer: 1 + PositiveModulo(seed, 3),
                Length: 2 + PositiveModulo(seed * 19, 14));
        }
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _starScroll += dt;

        if (ServiceLocator.Input.IsActionPressed(Action.StartGame))
        {
            ChangeScene("newGame");
        }

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Rectangle screen = new(0, 0, WorldConfig.ScreenWidth, WorldConfig.ScreenHeight);
        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), screen, Color.Black);
        DrawStars(spriteBatch);
        DrawCapsule(spriteBatch);

        MenuSceneVisuals.DrawCenteredText(spriteBatch, "MISSION COMPLETE", new Vector2(640f, 72f), new Color(236, 252, 252), 3f);
        MenuSceneVisuals.DrawDivider(spriteBatch, 640, 132, 342, new Color(154, 226, 236));
        MenuSceneVisuals.DrawCenteredText(spriteBatch, "The return capsule has launched.", new Vector2(640f, 160f), new Color(220, 238, 232), 1.2f);
        MenuSceneVisuals.DrawCenteredText(spriteBatch, "PRESS ENTER TO START AGAIN", new Vector2(640f, 626f), new Color(245, 232, 176), 1.1f);
        spriteBatch.End();
    }

    private void DrawStars(SpriteBatch spriteBatch)
    {
        Texture2D pixel = AssetManager.GetTexture(Art.pixel);
        for (int i = 0; i < _stars.Length; i++)
        {
            Star star = _stars[i];
            float speed = StarBaseSpeed + star.Layer * StarSpeedRange * 0.35f;
            int y = PositiveModulo((int)MathF.Round(star.Y + _starScroll * speed), WorldConfig.ScreenHeight + 80) - 40;
            int thickness = star.Layer == 3 ? 2 : 1;
            Color color = star.Layer == 3
                ? new Color(230, 248, 255, 220)
                : star.Layer == 2
                    ? new Color(190, 222, 235, 178)
                    : new Color(150, 170, 190, 130);

            spriteBatch.Draw(pixel, new Rectangle(star.X, y, thickness, star.Length), color);
        }
    }

    private static void DrawCapsule(SpriteBatch spriteBatch)
    {
        int x = (WorldConfig.ScreenWidth - CapsuleWidth) / 2;
        int y = 252;
        Rectangle upperBounds = new(x, y, CapsuleWidth, CapsuleHeight);

        spriteBatch.Draw(AssetManager.GetTexture(Art.ApolloUpperHalf), upperBounds, Color.White);
    }

    private static int PositiveModulo(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private readonly record struct Star(int X, int Y, int Layer, int Length);
}
