using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class StartScene : Scene
{
    public StartScene() : base()
    {
        AddEntity(new TextEntity(new Vector2(640, 180), "Vehicle Prototype", TextAlignment.TopCenter));
        AddEntity(new TextEntity(new Vector2(640, 240), "WASD / Arrows Move   Space Jump", TextAlignment.TopCenter));
        AddEntity(new TextEntity(new Vector2(640, 280), "E Toggle Power   Shift Toggle Camera", TextAlignment.TopCenter));
        AddEntity(new TextEntity(new Vector2(640, 320), "G Pick Up / Drop Fuel Barrel   F3 Toggle Debug", TextAlignment.TopCenter));
        AddEntity(new TextEntity(new Vector2(640, 360), "Press Enter or click Start", TextAlignment.TopCenter));
        AddEntity(new ButtonEntity(Art.Button, new Vector2(540, 460), "Start", "level1"));
    }

    public override void Update(GameTime gameTime)
    {
        if (ServiceLocator.Input.IsActionPressed(Action.StartGame))
        {
            ChangeScene("level1");
        }

        base.Update(gameTime);
    }
}
