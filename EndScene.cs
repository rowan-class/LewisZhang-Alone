using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace LewisZhang_Alone;

public class EndScene : Scene
{
    public EndScene() : base()
    {
        AddEntity(new TextEntity(new Vector2(520, 280), "You Win!"));
        AddEntity(new TextEntity(new Vector2(410, 320), "Press Enter to Restart"));
        AddEntity(new ButtonEntity(Art.Button, new Vector2(530, 500), "Restart", "start"));
    }

    public override void Update(GameTime gameTime)
    {
        KeyboardState keys = Keyboard.GetState();
        if (keys.IsKeyDown(Keys.Enter) || keys.IsKeyDown(Keys.R))
        {
            ChangeScene("start");
        }

        base.Update(gameTime);
    }
}

