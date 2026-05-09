using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class StartScene : Scene
{
    private const float BackgroundScrollSpeed = 95f;
    private const float SandstormScrollSpeed = 180f;
    private static readonly Rectangle VehiclePreviewBounds = new(240, 284, 800, 432);
    private static readonly float VehiclePreviewScale = VehiclePreviewBounds.Width / (float)WorldConfig.VehicleSize.X;

    private float _backgroundScrollX = 2050f;
    private float _sandstormScrollX;
    private readonly float[] _wheelInitialRotations = VehicleEntity.CreateWheelInitialRotations();
    private float _wheelRotation;

    public StartScene() : base()
    {
        AddEntity(new ButtonEntity(Art.Button, new Vector2(410, 506), "New Game", "newGame"));
        AddEntity(new ButtonEntity(Art.Button, new Vector2(670, 506), "Continue", "continue", SaveManager.HasSave));
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _backgroundScrollX += BackgroundScrollSpeed * dt;
        _sandstormScrollX += SandstormScrollSpeed * dt;
        _wheelRotation = MathHelper.WrapAngle(
            _wheelRotation + VehicleEntity.GetWheelRotationDelta(BackgroundScrollSpeed * dt, VehiclePreviewScale));

        if (ServiceLocator.Input.IsActionPressed(Action.StartGame))
        {
            ChangeScene("newGame");
        }

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawScrollingBackground(spriteBatch);
        spriteBatch.Draw(AssetManager.GetTexture(Art.Vehicle), VehiclePreviewBounds, Color.White * 0.9f);
        VehicleEntity.DrawWheels(
            spriteBatch,
            new Vector2(VehiclePreviewBounds.X, VehiclePreviewBounds.Y),
            VehiclePreviewScale,
            _wheelRotation,
            _wheelInitialRotations,
            Color.White * 0.9f);
        DrawScrollingSand(spriteBatch);
        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), new Rectangle(0, 0, WorldConfig.ScreenWidth, WorldConfig.ScreenHeight), new Color(7, 10, 14, 118));

        Rectangle panel = new(316, 78, 648, 210);
        MenuSceneVisuals.DrawPanel(spriteBatch, panel, new Color(9, 13, 18, 196), new Color(222, 190, 116, 210));
        MenuSceneVisuals.DrawCenteredText(spriteBatch, "ALONE", new Vector2(640f, 116f), new Color(248, 240, 212), 3.2f);
        MenuSceneVisuals.DrawDivider(spriteBatch, 640, 176, 360, new Color(230, 190, 106));
        MenuSceneVisuals.DrawCenteredText(spriteBatch, "Lewis", new Vector2(640f, 204f), new Color(196, 214, 222), 1.3f);
        MenuSceneVisuals.DrawCenteredText(spriteBatch, "Keep the machine alive. Keep moving forward.", new Vector2(640f, 236f), new Color(224, 220, 202), 1.1f);

        DrawEntities(spriteBatch, FindEntities<ButtonEntity>());
        MenuSceneVisuals.DrawCenteredText(spriteBatch, "PRESS ENTER TO LAUNCH A NEW RUN", new Vector2(640f, 604f), new Color(210, 218, 218), 1f);
        spriteBatch.End();
    }

    private void DrawScrollingBackground(SpriteBatch spriteBatch)
    {
        Texture2D background = AssetManager.GetTexture(Art.BackgroundDay);
        int sourceWidth = (int)MathF.Round(background.Height * (WorldConfig.ScreenWidth / (float)WorldConfig.ScreenHeight));
        sourceWidth = Math.Clamp(sourceWidth, 1, background.Width);
        int sourceX = PositiveModulo((int)MathF.Round(_backgroundScrollX), background.Width);
        DrawWrappedSource(spriteBatch, background, sourceX, sourceWidth, new Rectangle(0, 0, WorldConfig.ScreenWidth, WorldConfig.ScreenHeight), Color.White);
    }

    private void DrawScrollingSand(SpriteBatch spriteBatch)
    {
        Texture2D sandstorm = AssetManager.GetTexture(Art.Sandstorm);
        int sheetWidth = 1800;
        int startX = -PositiveModulo((int)MathF.Round(_sandstormScrollX), sheetWidth);

        for (int x = startX - sheetWidth; x < WorldConfig.ScreenWidth + sheetWidth; x += sheetWidth)
        {
            spriteBatch.Draw(sandstorm, new Rectangle(x, 0, sheetWidth, WorldConfig.ScreenHeight), Color.White * 0.13f);
        }
    }

    private static void DrawWrappedSource(SpriteBatch spriteBatch, Texture2D texture, int sourceX, int sourceWidth, Rectangle destination, Color tint)
    {
        int remainingSourceWidth = sourceWidth;
        int currentSourceX = sourceX;
        int destinationX = destination.X;
        int remainingDestinationWidth = destination.Width;

        while (remainingSourceWidth > 0 && remainingDestinationWidth > 0)
        {
            int segmentSourceWidth = Math.Min(remainingSourceWidth, texture.Width - currentSourceX);
            int segmentDestinationWidth = (int)MathF.Ceiling(destination.Width * (segmentSourceWidth / (float)sourceWidth));
            segmentDestinationWidth = Math.Min(segmentDestinationWidth, remainingDestinationWidth);

            Rectangle source = new(currentSourceX, 0, segmentSourceWidth, texture.Height);
            Rectangle segmentDestination = new(destinationX, destination.Y, segmentDestinationWidth, destination.Height);
            spriteBatch.Draw(texture, segmentDestination, source, tint);

            remainingSourceWidth -= segmentSourceWidth;
            remainingDestinationWidth -= segmentDestinationWidth;
            destinationX += segmentDestinationWidth;
            currentSourceX = 0;
        }
    }

    private static int PositiveModulo(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}
