using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class EnemyEntity : SpriteEntity
{
    private const float Speed = 2.0f;
    private float _direction = 1f;

    public EnemyEntity(Vector2 position) : base(Art.Enemy1, position)
    {
        _velocity = new Vector2(Speed, 0);
    }

    public override void Update(GameTime gameTime)
    {
        if (!_isActive)
        {
            return;
        }

        // Patrol horizontally.
        // _velocity = new Vector2(Speed * _direction, 0);
        // _position += _velocity;

        // float maxX = Game1.ScreenSize.X - (_texture?.Width ?? 0);
        // if (_position.X < 0)
        // {
        //     _position.X = 0;
        //     _direction = 1f;
        // }
        // else if (_position.X > maxX)
        // {
        //     _position.X = maxX;
        //     _direction = -1f;
        // }

        if (_scene != null)
        {
            Player player = _scene.FindFirstEntity<Player>();
            if (player != null && CheckCollision(player))
            {
                Rectangle enemyBounds = GetBounds();
                Rectangle playerBounds = player.GetBounds();
                Rectangle intersection = Rectangle.Intersect(playerBounds, enemyBounds);

                bool stompFromTop =
                    intersection.Height > 0
                    && intersection.Width > 0
                    && intersection.Height <= intersection.Width
                    && playerBounds.Center.Y <= enemyBounds.Center.Y;

                if (stompFromTop)
                {
                    Deactivate();
                    return;
                }

                _scene.RestartLevel();
                return;
            }
        }

        _rect.X = (int)_position.X;
        _rect.Y = (int)_position.Y;
    }
}
