using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace LewisZhang_Alone;

public class LevelSelectScene : Scene
{
    public LevelSelectScene() : base()
    {
        AddEntity(new TextEntity(new Vector2(520, 220), "Select Level"));
        AddEntity(new TextEntity(new Vector2(430, 260), "Press 1 or 2, or click a button"));
        AddEntity(new TextEntity(new Vector2(440, 300), "Backspace to return to menu"));

        AddEntity(new ButtonEntity(Art.Button, new Vector2(540, 360), "Level 1", "level1"));
        AddEntity(new ButtonEntity(Art.Button, new Vector2(540, 440), "Level 2", "level2"));
        AddEntity(new ButtonEntity(Art.Button, new Vector2(540, 520), "Back", "start"));
    }

    public override void Update(GameTime gameTime)
    {
        KeyboardState keys = Keyboard.GetState();
        if (keys.IsKeyDown(Keys.D1) || keys.IsKeyDown(Keys.NumPad1))
        {
            ChangeScene("level1");
        }
        else if (keys.IsKeyDown(Keys.D2) || keys.IsKeyDown(Keys.NumPad2))
        {
            ChangeScene("level2");
        }
        else if (keys.IsKeyDown(Keys.Back))
        {
            ChangeScene("start");
        }

        base.Update(gameTime);
    }
}
