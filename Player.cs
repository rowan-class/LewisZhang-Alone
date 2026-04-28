using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public enum PlayerState
{
    Grounded,
    Jumping,
    Falling,
}

public class Player : PhysicsEntity
{
    protected float speed = 2.5f;
    protected float jumpPower = 10f;
    protected PlayerState state;

    public PlayerState CurrentState => state;
    public float CurrentConfiguredSpeed => speed;

    private float _groundSpeed = 2.5f;
    private float _airSpeed = 2f;
    private Art _facingArt = Art.Player1;

    private bool wantsToJump = false;
    private Vector2 desiredDirection = Vector2.Zero;

    public Player(Art art, Vector2 position) : base(art, position)
    {
        ApplyDefaultMovement();
        ChangeState(PlayerState.Falling);
    }

    public override void Update(GameTime gameTime)
    {
        wantsToJump = false;
        desiredDirection = Vector2.Zero;
        acceleration = Vector2.Zero;

        HandleInput();
        UpdateStateFromMovement();

        if (wantsToJump && isGrounded)
        {
            velocity.Y = -jumpPower;
            isGrounded = false;
            ChangeState(PlayerState.Jumping);
        }

        if (desiredDirection.X != 0)
        {
            UpdateFacingSprite();
            acceleration.X += desiredDirection.X * speed;
        }

        base.Update(gameTime);
        KeepInsideHorizontalScreenBounds();
        UpdateStateFromMovement();
    }

    private void ApplyDefaultMovement()
    {
        jumpPower = 10f;
        _groundSpeed = 2.5f;
        _airSpeed = 2f;
        SetPhysicsTuning(
            newFriction: 0.85f,
            newMaxSpeedX: 5f,
            newMaxRiseSpeed: 50f,
            newMaxFallSpeed: 50f,
            newGravity: new Vector2(0f, 1f));
    }

    private void UpdateStateFromMovement()
    {
        if (isGrounded)
        {
            ChangeState(PlayerState.Grounded);
            return;
        }

        if (velocity.Y < 0)
        {
            ChangeState(PlayerState.Jumping);
            return;
        }

        ChangeState(PlayerState.Falling);
    }

    private void UpdateFacingSprite()
    {
        if (desiredDirection.X > 0)
        {
            _facingArt = Art.Player1;
        }
        else if (desiredDirection.X < 0)
        {
            _facingArt = Art.Player2;
        }

        _texture = AssetManager.GetTexture(_facingArt);
    }

    private void KeepInsideHorizontalScreenBounds()
    {
        if (_texture == null)
        {
            return;
        }

        float maxX = Game1.ScreenSize.X - _texture.Width;
        _position.X = MathHelper.Clamp(_position.X, 0f, maxX);
    }

    private void HandleInput()
    {
        if (ServiceLocator.Input.IsActionPressed(Action.Jump))
        {
            wantsToJump = true;
        }

        if (ServiceLocator.Input.IsActionDown(Action.MoveRight))
        {
            desiredDirection.X += 1;
        }

        if (ServiceLocator.Input.IsActionDown(Action.MoveLeft))
        {
            desiredDirection.X -= 1;
        }
    }

    private void ChangeState(PlayerState newState)
    {
        state = newState;

        switch (state)
        {
            case PlayerState.Grounded:
                speed = _groundSpeed;
                break;
            case PlayerState.Jumping:
            case PlayerState.Falling:
                speed = _airSpeed;
                break;
        }
    }
}
