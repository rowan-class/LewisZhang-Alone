using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class SpikeEntity : SpriteEntity
{
    private const int Damage = 1;
    private bool _hasHitPlayer;

    public SpikeEntity(Vector2 position) : base(Art.Spike, position)
    {
    }

    public override void Update(GameTime gameTime)
    {
        if (_hasHitPlayer || _scene == null)
        {
            return;
        }

        Player player = _scene.FindFirstEntity<Player>();
        if (player == null)
        {
            return;
        }

        if (CheckCollision(player))
        {
            _hasHitPlayer = true;
            ServiceLocator.GameState?.DamageHp(Damage);
            Deactivate(); // disappears and can't hurt again
        }
    }
}

