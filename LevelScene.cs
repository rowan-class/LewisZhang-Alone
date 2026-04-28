using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class LevelScene : Scene
{
    private readonly string _currentSceneKey;
    private readonly TextEntity _hpText;

    public LevelScene(string levelName, string currentSceneKey) : base(levelName)
    {
        _currentSceneKey = currentSceneKey;
        _hpText = new TextEntity(new Vector2(10, 10), "");
        AddEntity(_hpText);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        int hp = ServiceLocator.GameState?.GetHp() ?? 0;

        if (hp <= 0)
        {
            ChangeScene("start");
            return;
        }

        Player player = FindFirstEntity<Player>();
        if (player == null)
        {
            _hpText.SetText("HP: " + hp + "   State: N/A   Speed: 0.00");
            return;
        }

        _hpText.SetText("HP: " + hp + "   State: " + player.CurrentState + "   Speed: " + player.CurrentConfiguredSpeed.ToString("0.00"));

        Rectangle playerBounds = player.GetBounds();
        if (playerBounds.Top > Game1.ScreenSize.Y)
        {
            ChangeScene(_currentSceneKey);
        }
    }

    public override void RestartLevel()
    {
        ChangeScene(_currentSceneKey);
    }
}
