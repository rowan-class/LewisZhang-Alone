using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace LewisZhang_Alone;

public class StartScene : Scene
{
    public StartScene() : base()
    {
        AddEntity(new TextEntity(new Vector2(480, 240), "Assignment 3"));
        AddEntity(new TextEntity(new Vector2(420, 280), "Move with WASD or Arrow Keys"));
        AddEntity(new TextEntity(new Vector2(470, 320), "Reach the yellow exit"));
        AddEntity(new TextEntity(new Vector2(455, 360), "Press L for Level Select"));
        AddEntity(new ButtonEntity(Art.Button, new Vector2(540, 420), "Start (Level 1)", "level1"));
        AddEntity(new ButtonEntity(Art.Button, new Vector2(540, 600), "Level Select", "level_select"));
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
        else if (keys.IsKeyDown(Keys.L))
        {
            ChangeScene("level_select");
        }

        base.Update(gameTime);
    }
}
