using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace LewisZhang_Alone;

public class StartScene : Scene
{
    public StartScene() : base()
    {
        AddEntity(new TextEntity(new Vector2(640, 240), "Assignment 3", TextAlignment.TopCenter));
        AddEntity(new TextEntity(new Vector2(640, 280), "Move with WASD or Arrow Keys", TextAlignment.TopCenter));
        AddEntity(new TextEntity(new Vector2(640, 320), "Press Space to Jump", TextAlignment.TopCenter));
        AddEntity(new TextEntity(new Vector2(640, 360), "Press Enter or click Start", TextAlignment.TopCenter));
        AddEntity(new ButtonEntity(Art.Button, new Vector2(540, 460), "Start", "level1"));
    }

    public override void Open()
    {
        ServiceLocator.GameState?.ResetHp();
    }

    public override void Update(GameTime gameTime)
    {
        KeyboardState keys = Keyboard.GetState();
        if (keys.IsKeyDown(Keys.Enter) || keys.IsKeyDown(Keys.G))
        {
            ChangeScene("level1");
        }

        base.Update(gameTime);
    }
}
