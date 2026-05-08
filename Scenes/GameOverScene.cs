using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class GameOverScene : Scene
{
    public GameOverScene()
    {
        AddEntity(new TextEntity(new Vector2(640, 240), "Mission Failed", TextAlignment.TopCenter));
        AddEntity(new TextEntity(new Vector2(640, 300), "Too many rover systems stayed broken.", TextAlignment.TopCenter));
        AddEntity(new TextEntity(new Vector2(640, 380), "Press Enter to Try Again", TextAlignment.TopCenter));
    }

    public override void Update(GameTime gameTime)
    {
        if (ServiceLocator.Input.IsActionPressed(Action.StartGame))
        {
            ChangeScene("newGame");
        }

        base.Update(gameTime);
    }
}
