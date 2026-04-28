using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public enum PlayerState
{
    Grounded,
    Up,
    Jumping,
    Falling,
}

public enum PlayerTuning
{
    Default = 1,
    Floaty = 2,
    Heavy = 3
}


public class Player : PhysicsEntity
{
    protected float speed = 2.5f;
    protected float jumpPower = 15f;
    protected PlayerState state;

    private PlayerTuning _activeTuning = PlayerTuning.Default;
    public int ActiveTuningNumber => (int)_activeTuning;
    public PlayerState CurrentState => state;
    public float CurrentConfiguredSpeed => speed;
    private float _groundSpeed = 2.5f;
    private float _jumpSpeed = 2f;
    private float _fallSpeed = 1f;
    private const float UpStateDuration = 15f; // Duration in frames for the "Up" state
    private float upcounter = 0f;
    private Art _facingArt = Art.Player1;

    bool wantsToJump = false;
    Vector2 desiredDirection = Vector2.Zero;

    public Player(Art art, Vector2 position) : base(art, position)
    {
        ApplyTuning(PlayerTuning.Default);
        ChangeState(PlayerState.Falling);
    }

    public override void Update(GameTime gameTime)
    {
        wantsToJump = false;
        desiredDirection = Vector2.Zero;
        acceleration = Vector2.Zero;

        switch (state)
        {
            case PlayerState.Up:
                // This state is not used in the current implementation, but could be for a "rising" state if desired.
                if (isGrounded){
                    ChangeState(PlayerState.Grounded);
                    break;
                }else if (!ServiceLocator.Input.IsActionDown(Action.Jump)|| upcounter > UpStateDuration){
                    ChangeState(PlayerState.Jumping);
                    break;
                }
                HandleInput();
                break;
            case PlayerState.Grounded:
                if (isGrounded == false){
                    if (velocity.Y < 0){
                        ChangeState(PlayerState.Up);
                        break;
                    }else{
                        ChangeState(PlayerState.Falling);
                        break;
                    }
                }
                HandleInput();
                break;

            case PlayerState.Jumping:
                if (isGrounded){
                    ChangeState(PlayerState.Grounded);
                    break;
                }
                HandleInput();
                break;

            case PlayerState.Falling:
                if (isGrounded){
                    ChangeState(PlayerState.Grounded);
                    break;
                }
                HandleInput();
                break;
        }

        if (wantsToJump && isGrounded){
            velocity.Y = -jumpPower;
            ChangeState(PlayerState.Up);
            upcounter = 0f;
        }

        if  (state == PlayerState.Up){
            // Allow for a small amount of horizontal control while rising, but less than when falling or grounded.
            velocity.Y = -jumpPower;
            upcounter += 1f;
        }

        if (desiredDirection.X != 0){
            UpdateFacingSprite();
            acceleration.X += desiredDirection.X * speed;
        }

        base.Update(gameTime);
        KeepInsideHorizontalScreenBounds();
    }

    void UpdateFacingSprite()
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

    void KeepInsideHorizontalScreenBounds()
    {
        if (_texture == null){return;}
        float maxX = Game1.ScreenSize.X - _texture.Width;
        _position.X = MathHelper.Clamp(_position.X, 0f, maxX);
    }

    void HandleInput()
    {
        HandleTuningInput();
        if (ServiceLocator.Input.IsActionPressed(Action.Jump))   wantsToJump = true;
        if (ServiceLocator.Input.IsActionDown(Action.MoveRight)) desiredDirection.X += 1;
        if (ServiceLocator.Input.IsActionDown(Action.MoveLeft))  desiredDirection.X -= 1;
    }

    void HandleTuningInput()
    {
        if (ServiceLocator.Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.D1) || ServiceLocator.Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.NumPad1)) {
            ApplyTuning(PlayerTuning.Default);
            return;
        }
        if (ServiceLocator.Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.D2) || ServiceLocator.Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.NumPad2)){
            ApplyTuning(PlayerTuning.Floaty);
            return;
        }
        if (ServiceLocator.Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.D3) || ServiceLocator.Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.NumPad3)){
            ApplyTuning(PlayerTuning.Heavy);
        }
    }

    void ApplyTuning(PlayerTuning tuning)
    {
        _activeTuning = tuning;

        switch (_activeTuning)
        {
            case PlayerTuning.Default:
                // Original Floaty tuning becomes the new default
                jumpPower = 13f;
                _groundSpeed = 0.22f;
                _jumpSpeed = 2.0f;
                _fallSpeed = 1.6f;
                SetPhysicsTuning(
                    newFriction: 0.96f,
                    newMaxSpeedX: 5f,
                    newMaxRiseSpeed: 30f,
                    newMaxFallSpeed: 18f,
                    newGravity: new Vector2(0f, 1f));
                break;

            case PlayerTuning.Floaty:
                // New extreme floaty tuning: very light gravity, high jump, slow fall
                jumpPower = 15f;
                _groundSpeed = 3.2f;
                _jumpSpeed = 3.5f;
                _fallSpeed = 3.0f;
                SetPhysicsTuning(
                    newFriction: 0.98f,
                    newMaxSpeedX: 7f,
                    newMaxRiseSpeed: 20f,
                    newMaxFallSpeed: 12f,
                    newGravity: new Vector2(0f, 0.25f));
                break;

            case PlayerTuning.Heavy:
                // Original Default tuning becomes Heavy
                jumpPower = 10f;
                _groundSpeed = 2.5f;
                _jumpSpeed = 2f;
                _fallSpeed = 1f;
                SetPhysicsTuning(
                    newFriction: 0.85f,
                    newMaxSpeedX: 5f,
                    newMaxRiseSpeed: 50f,
                    newMaxFallSpeed: 50f,
                    newGravity: new Vector2(0f, 1f));
                break;
        }

        ChangeState(state);
    }

    void ChangeState(PlayerState newState)
    {
        // exiting state

        state = newState;

        // entering state
        switch (state){
            case PlayerState.Grounded:
                speed = _groundSpeed;
                break;
            case PlayerState.Up:
                speed = _jumpSpeed;
                break;
            case PlayerState.Falling:
                speed = _fallSpeed;
                break;
            case PlayerState.Jumping:
                speed = _jumpSpeed;
                break;
        }
    }
}
