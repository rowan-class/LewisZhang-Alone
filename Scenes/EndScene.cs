using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class EndScene : Scene
{
    public EndScene()
    {
        AddEntity(new TextEntity(new Vector2(640, 240), "Mission Complete", TextAlignment.TopCenter));
        AddEntity(new TextEntity(new Vector2(640, 300), "The return capsule has launched.", TextAlignment.TopCenter));
        AddEntity(new TextEntity(new Vector2(640, 380), "Press Enter to Start Again", TextAlignment.TopCenter));
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
