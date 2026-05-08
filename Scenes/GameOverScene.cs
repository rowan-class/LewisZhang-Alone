using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class GameOverScene : Scene
{
    private const int SandstormLayerCount = 3;
    private const int SandstormSheetWidth = 1800;
    private const float SandstormBaseSpeed = 250f;

    private float _sandstormScroll;

    public GameOverScene()
    {
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _sandstormScroll += SandstormBaseSpeed * dt;

        if (ServiceLocator.Input.IsActionPressed(Action.StartGame))
        {
            ChangeScene("newGame");
        }

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        MenuSceneVisuals.DrawBackdrop(spriteBatch, Art.BackgroundNight, 5200, new Color(20, 5, 5, 154));
        DrawCapsuleOnMars(spriteBatch);
        DrawSandstorm(spriteBatch);
        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), new Rectangle(0, 0, WorldConfig.ScreenWidth, WorldConfig.ScreenHeight), new Color(92, 8, 10, 76));

        Rectangle panel = new(340, 126, 600, 250);
        MenuSceneVisuals.DrawPanel(spriteBatch, panel, new Color(18, 10, 12, 210), new Color(210, 76, 58, 220));
        MenuSceneVisuals.DrawCenteredText(spriteBatch, "MISSION FAILED", new Vector2(640f, 172f), new Color(255, 222, 202), 3f);
        MenuSceneVisuals.DrawDivider(spriteBatch, 640, 232, 330, new Color(218, 78, 58));
        MenuSceneVisuals.DrawCenteredText(spriteBatch, "Too many rover systems stayed broken.", new Vector2(640f, 264f), new Color(236, 220, 210), 1.25f);
        MenuSceneVisuals.DrawCenteredText(spriteBatch, "PRESS ENTER TO TRY AGAIN", new Vector2(640f, 332f), new Color(255, 204, 118), 1.1f);
        spriteBatch.End();
    }

    private void DrawSandstorm(SpriteBatch spriteBatch)
    {
        Texture2D sandstorm = AssetManager.GetTexture(Art.Sandstorm);

        for (int layer = SandstormLayerCount - 1; layer >= 0; layer--)
        {
            float layerSpeed = 0.72f + layer * 0.18f;
            float offset = PositiveModulo(_sandstormScroll * layerSpeed + layer * 430f, SandstormSheetWidth);
            int startX = (int)MathF.Round(-offset);
            Color tint = new Color(226, 120, 78) * (0.18f + layer * 0.07f);

            for (int x = startX - SandstormSheetWidth; x < WorldConfig.ScreenWidth + SandstormSheetWidth; x += SandstormSheetWidth)
            {
                Rectangle destination = new(x, 0, SandstormSheetWidth, WorldConfig.ScreenHeight);
                spriteBatch.Draw(sandstorm, destination, tint);
            }
        }
    }

    private static void DrawCapsuleOnMars(SpriteBatch spriteBatch)
    {
        Rectangle capsuleBounds = new(512, 344, 256, 256);
        Color capsuleTint = new Color(255, 205, 185);
        spriteBatch.Draw(AssetManager.GetTexture(Art.ApolloUpperHalf), capsuleBounds, capsuleTint);
        spriteBatch.Draw(AssetManager.GetTexture(Art.ApolloLowerHalf), capsuleBounds, capsuleTint);
    }

    private static float PositiveModulo(float value, float modulus)
    {
        float result = value % modulus;
        return result < 0f ? result + modulus : result;
    }
}
