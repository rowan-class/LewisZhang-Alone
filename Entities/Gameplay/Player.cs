using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
    private const float AnimationFrameSeconds = 0.1f;

    private bool _isFacingLeft;
    private bool _isMovingHorizontally;
    private bool _isCarrying;
    private float _animationTimer;
    private PlayerState _state = PlayerState.Falling;

    public PlayerState CurrentState => _state;
    public float CurrentConfiguredSpeed => isGrounded ? GroundMoveSpeed : AirMoveSpeed;
    public bool IsFacingLeft => _isFacingLeft;

    public Player(Vector2 position)
        : base(Art.Player1, position, WorldConfig.PlayerSize)
    {
    }

    public override void Update(GameTime gameTime)
    {
        if (_scene is not IPlayerScene playerScene)
        {
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float movementInput = 0f;

        bool inputLocked = playerScene.IsPlayerInputLocked;

        if (!inputLocked && ServiceLocator.Input.IsActionDown(Action.MoveRight))
        {
            movementInput += 1f;
        }

        if (!inputLocked && playerScene.CanPlayerMoveLeft && ServiceLocator.Input.IsActionDown(Action.MoveLeft))
        {
            movementInput -= 1f;
        }

        _isMovingHorizontally = movementInput != 0f;
        _isCarrying = playerScene.IsPlayerCarryingItem;

        if (movementInput > 0f)
        {
            _isFacingLeft = false;
        }
        else if (movementInput < 0f)
        {
            _isFacingLeft = true;
        }

        if (!playerScene.IsPlayerAttachedToVehicle(this))
        {
            _position.X -= playerScene.VehicleSpeed * dt;
        }

        velocity.X = movementInput * (isGrounded ? GroundMoveSpeed : AirMoveSpeed);

        if (!inputLocked && ServiceLocator.Input.IsActionPressed(Action.Jump) && isGrounded)
        {
            velocity.Y = -JumpSpeed;
            isGrounded = false;
        }

        SimulatePhysics(dt);
        KeepInsideWorldBounds();
        UpdateState();
        UpdateAnimation(dt);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!_isActive || _texture == null)
        {
            return;
        }

        SpriteEffects effects = _isFacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        spriteBatch.Draw(_texture, GetBounds(), null, _tint, 0f, Vector2.Zero, effects, 0f);
    }

    protected override IEnumerable<Rectangle> GetSolidRectangles()
    {
        if (_scene is IPlayerScene playerScene)
        {
            return playerScene.GetSolidRectangles();
        }

        return System.Array.Empty<Rectangle>();
    }

    public Vector2 GetCarryAnchor(Point carriedItemSize)
    {
        float anchorY = _position.Y + 10f;

        if (_isFacingLeft)
        {
            return new Vector2(_position.X - carriedItemSize.X + 2f, anchorY);
        }

        return new Vector2(_position.X + _size.X - 2f, anchorY);
    }

    private void KeepInsideWorldBounds()
    {
        Rectangle movementBounds = _scene is IPlayerScene playerScene
            ? playerScene.PlayerMovementBounds
            : WorldConfig.OverviewViewBounds;

        if (_position.X < movementBounds.Left)
        {
            _position.X = movementBounds.Left;
        }

        float maxX = movementBounds.Right - _size.X;
        if (_position.X > maxX)
        {
            _position.X = maxX;
        }

        if (_position.Y < movementBounds.Top)
        {
            _position.Y = movementBounds.Top;
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

    private void UpdateAnimation(float dt)
    {
        if (_isMovingHorizontally && isGrounded)
        {
            _animationTimer += dt;
        }
        else
        {
            _animationTimer = 0f;
        }

        _texture = AssetManager.GetTexture(GetAnimationArt());
    }

    private Art GetAnimationArt()
    {
        if (!isGrounded)
        {
            return Art.Player5;
        }

        bool useSecondFrame = _isMovingHorizontally && (int)(_animationTimer / AnimationFrameSeconds) % 2 == 1;

        if (_isCarrying)
        {
            return useSecondFrame ? Art.Player4 : Art.Player3;
        }

        return useSecondFrame ? Art.Player2 : Art.Player1;
    }
}
