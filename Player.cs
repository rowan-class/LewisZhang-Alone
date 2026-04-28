using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public enum PlayerState
{
    Grounded,
    Jumping,
    Falling
}

public class Player : PhysicsEntity
{
    private const float GroundMoveSpeed = 320f;
    private const float AirMoveSpeed = 250f;
    private const float JumpSpeed = 720f;

    private Art _facingArt = Art.Player1;
    private PlayerState _state = PlayerState.Falling;

    public PlayerState CurrentState => _state;
    public float CurrentConfiguredSpeed => isGrounded ? GroundMoveSpeed : AirMoveSpeed;

    public Player(Vector2 position)
        : base(Art.Player1, position, WorldConfig.PlayerSize)
    {
    }

    public override void Update(GameTime gameTime)
    {
        if (_scene is not LevelScene levelScene)
        {
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float movementInput = 0f;

        if (ServiceLocator.Input.IsActionDown(Action.MoveRight))
        {
            movementInput += 1f;
        }

        if (ServiceLocator.Input.IsActionDown(Action.MoveLeft))
        {
            movementInput -= 1f;
        }

        if (movementInput > 0f)
        {
            _facingArt = Art.Player1;
        }
        else if (movementInput < 0f)
        {
            _facingArt = Art.Player2;
        }

        _texture = AssetManager.GetTexture(_facingArt);

        if (!levelScene.IsPlayerAttachedToVehicle(this))
        {
            _position.X -= levelScene.VehicleSpeed * dt;
        }

        velocity.X = movementInput * (isGrounded ? GroundMoveSpeed : AirMoveSpeed);

        if (ServiceLocator.Input.IsActionPressed(Action.Jump) && isGrounded)
        {
            velocity.Y = -JumpSpeed;
            isGrounded = false;
        }

        SimulatePhysics(dt);
        KeepInsideWorldBounds();
        UpdateState();
    }

    protected override IEnumerable<Rectangle> GetSolidRectangles()
    {
        if (_scene is LevelScene levelScene)
        {
            return levelScene.GetSolidRectangles();
        }

        return System.Array.Empty<Rectangle>();
    }

    public Vector2 GetCarryAnchor()
    {
        return new Vector2(_position.X + _size.X + 8f, _position.Y + 10f);
    }

    private void KeepInsideWorldBounds()
    {
        if (_position.X < 0f)
        {
            _position.X = 0f;
        }

        float maxX = WorldConfig.WorldWidth - _size.X;
        if (_position.X > maxX)
        {
            _position.X = maxX;
        }

        if (_position.Y < 0f)
        {
            _position.Y = 0f;
            velocity.Y = 0f;
        }
    }

    private void UpdateState()
    {
        if (isGrounded)
        {
            _state = PlayerState.Grounded;
        }
        else if (velocity.Y < 0f)
        {
            _state = PlayerState.Jumping;
        }
        else
        {
            _state = PlayerState.Falling;
        }
    }
}
