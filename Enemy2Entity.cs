using System;
using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class Enemy2Entity : SpriteEntity
{
    private const float HorizontalSpeed = 2.5f;
    private const float VerticalAmplitude = 48f;
    private const float VerticalFrequency = 2.0f;

    private float _directionX = 1f;
    private float _time;
    private readonly float _baseY;

    public Enemy2Entity(Vector2 position) : base(Art.Enemy2, position)
    {
        _baseY = position.Y;
    }

    public override void Update(GameTime gameTime)
    {
        if (!_isActive)
        {
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _position.X += HorizontalSpeed * _directionX;
        float maxX = Game1.ScreenSize.X - (_texture?.Width ?? 0);
        if (_position.X <= 0)
        {
            _position.X = 0;
            _directionX = 1f;
        }
        else if (_position.X >= maxX)
        {
            _position.X = maxX;
            _directionX = -1f;
        }

        _time += dt;
        _position.Y = _baseY + (float)Math.Sin(_time * VerticalFrequency * MathF.PI * 2f) * VerticalAmplitude;

        if (_scene != null)
        {
            Player player = _scene.FindFirstEntity<Player>();
            if (player != null && CheckCollision(player))
            {
                _scene.RestartLevel();
                return;
            }
        }

        _rect.X = (int)_position.X;
        _rect.Y = (int)_position.Y;
    }
}
