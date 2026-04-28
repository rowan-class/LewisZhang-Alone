using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class LevelScene : Scene
{
    private readonly string _currentSceneKey;
    private readonly string _nextSceneKey;
    private readonly TextEntity _hpText;

    public LevelScene(string levelName, string currentSceneKey, string nextSceneKey) : base(levelName)
    {
        _currentSceneKey = currentSceneKey;
        _nextSceneKey = nextSceneKey;
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
            _hpText.SetText("HP: " + hp + "   Tuning: 1   State: N/A   Speed: 0.00" +"\nDefault = 1,\nFloaty = 2,\nHeavy = 3");
            return;
        }

        _hpText.SetText("HP: " + hp + "   Tuning: " + player.ActiveTuningNumber + "   State: " + player.CurrentState + "   Speed: " + player.CurrentConfiguredSpeed.ToString("0.00") +"\nDefault = 1,\nFloaty = 2,\nHeavy = 3");

        Rectangle playerBounds = player.GetBounds();
        if (playerBounds.Top > Game1.ScreenSize.Y)
        {
            ChangeScene(_currentSceneKey);
            return;
        }

        Rectangle bounds = playerBounds;
        Vector2 center = new Vector2(bounds.Center.X, bounds.Center.Y);
        Tile tile = Grid.GetTile(center);
        if (tile != null && tile.Type == TileType.Exit)
        {
            ChangeScene(_nextSceneKey);
        }
    }

    public override void RestartLevel()
    {
        ChangeScene(_currentSceneKey);
    }
}
