using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public enum ThrottleState
{
    Idle,
    Pushing,
    ActiveHold,
    SlowReturn,
    FastReturn
}

public class ThrottleEntity : SpaceEntity
{
    private const float HoldDuration = 6f;
    private const float SlowReturnSpeed = 0.45f;
    private const float FastReturnSpeed = 2.6f;
    private const float PlayerContactOverlap = 2f;
    private const int ContactPadding = 8;
    private const float DamageBlinkInterval = 0.18f;

    private readonly VehicleEntity _vehicle;
    private ThrottleState _state = ThrottleState.Idle;
    private float _leverPosition;
    private float _holdTimer;
    private bool _isPlayerPushing;
    private float _damageBlinkTimer;

    public ThrottleState State => _state;
    public float LeverPosition => _leverPosition;
    public bool IsPlayerPushing => _isPlayerPushing;
    public bool IsDamaged { get; private set; }

    public ThrottleEntity(VehicleEntity vehicle)
        : base(PositionSpace.Vehicle, WorldConfig.ThrottleIdleTopLeftLocal, WorldConfig.ThrottleSize)
    {
        _vehicle = vehicle;
    }

    public override void Update(GameTime gameTime)
    {
        Rectangle previousBounds = GetBounds();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _damageBlinkTimer += dt;
        bool isLockedByEvent = _scene is LevelScene throttleScene && throttleScene.IsThrottleLockedByEvent;
        bool hasFuel = _vehicle.Fuel > 0f;
        bool inputPush = !IsDamaged && !isLockedByEvent && IsPushInputActive();

        if (IsDamaged)
        {
            if (_state != ThrottleState.FastReturn && _state != ThrottleState.Idle)
            {
                ForceFastReturn();
            }
        }
        else if (isLockedByEvent)
        {
            ForceFastReturn();
        }
        else if (_vehicle.HandbrakeActive)
        {
            ForceFastReturn();
        }
        else if (!hasFuel)
        {
            if (_state != ThrottleState.Idle)
            {
                _state = ThrottleState.FastReturn;
            }
        }
        else if (inputPush && _state != ThrottleState.ActiveHold)
        {
            _state = ThrottleState.Pushing;
        }

        bool hasPower = UpdateState(dt, inputPush) && !IsDamaged;
        _isPlayerPushing = !IsDamaged
            && !isLockedByEvent
            && hasFuel
            && inputPush
            && (_state == ThrottleState.Pushing || _state == ThrottleState.ActiveHold);
        _vehicle.SetPowered(hasPower);
        UpdateLeverPosition();
        ResolvePlayerOverlapAfterMovement(previousBounds);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(AssetManager.GetTexture(Art.Throttle), GetBounds(), GetDamageTint());
    }

    public Rectangle GetCollisionBounds()
    {
        return GetBounds();
    }

    public void ForceFastReturn()
    {
        _holdTimer = 0f;
        _isPlayerPushing = false;
        _state = _leverPosition > 0f ? ThrottleState.FastReturn : ThrottleState.Idle;
    }

    public void Damage()
    {
        IsDamaged = true;
        ForceFastReturn();
        _vehicle.SetPowered(false);
    }

    public void Repair()
    {
        IsDamaged = false;
    }

    public ThrottleSaveData CaptureSaveData()
    {
        return new ThrottleSaveData
        {
            State = _state,
            LeverPosition = _leverPosition,
            HoldTimer = _holdTimer
        };
    }

    public void RestoreSaveData(ThrottleSaveData data)
    {
        if (data == null)
        {
            return;
        }

        _state = data.State;
        _leverPosition = MathHelper.Clamp(data.LeverPosition, 0f, 1f);
        _holdTimer = System.MathF.Max(0f, data.HoldTimer);
        UpdateLeverPosition();
    }

    public override IEnumerable<Rectangle> GetDebugRectangles()
    {
        foreach (Rectangle rect in base.GetDebugRectangles())
        {
            yield return rect;
        }

        if (!IsDamaged)
        {
            yield return GetPushContactBounds();
            yield return GetInteractionBounds();
        }
    }

    private bool UpdateState(float dt, bool inputPush)
    {
        switch (_state)
        {
            case ThrottleState.Idle:
                _leverPosition = 0f;
                return false;

            case ThrottleState.Pushing:
                if (!inputPush)
                {
                    _state = ThrottleState.FastReturn;
                    return false;
                }

                _leverPosition = GetPlayerDrivenLeverPosition();
                BlockPlayerAtLever();

                if (_leverPosition >= 1f)
                {
                    _leverPosition = 1f;
                    _holdTimer = HoldDuration;
                    _state = ThrottleState.ActiveHold;
                    BlockPlayerAtLever();
                    return true;
                }

                return false;

            case ThrottleState.ActiveHold:
                _leverPosition = 1f;
                if (inputPush)
                {
                    BlockPlayerAtLever();
                }

                _holdTimer -= dt;
                if (_holdTimer <= 0f)
                {
                    _state = ThrottleState.SlowReturn;
                }

                return true;

            case ThrottleState.SlowReturn:
                _leverPosition = MathHelper.Clamp(_leverPosition - SlowReturnSpeed * dt, 0f, 1f);
                if (_leverPosition <= 0f)
                {
                    _leverPosition = 0f;
                    _state = ThrottleState.Idle;
                    return false;
                }

                return true;

            case ThrottleState.FastReturn:
                _leverPosition = MathHelper.Clamp(_leverPosition - FastReturnSpeed * dt, 0f, 1f);
                if (_leverPosition <= 0f)
                {
                    _leverPosition = 0f;
                    _state = ThrottleState.Idle;
                }

                return false;

            default:
                return false;
        }
    }

    private bool IsPushInputActive()
    {
        if (_scene is not LevelScene levelScene)
        {
            return false;
        }

        return GetPushContactBounds().Intersects(levelScene.PlayerBounds)
            && ServiceLocator.Input.IsActionDown(Action.Interact)
            && ServiceLocator.Input.IsActionDown(Action.MoveRight);
    }

    private float GetPlayerDrivenLeverPosition()
    {
        if (_scene is not LevelScene levelScene)
        {
            return _leverPosition;
        }

        float idleX = GetIdleWorldX(levelScene);
        float pushedPixels = levelScene.PlayerBounds.Right + PlayerContactOverlap - idleX;
        return MathHelper.Clamp(
            System.MathF.Max(_leverPosition * WorldConfig.ThrottleTravelDistance, pushedPixels) / WorldConfig.ThrottleTravelDistance,
            0f,
            1f);
    }

    private void BlockPlayerAtLever()
    {
        if (_scene is not LevelScene levelScene)
        {
            return;
        }

        Rectangle playerBounds = levelScene.PlayerBounds;
        float contactX = GetIdleWorldX(levelScene) + WorldConfig.ThrottleTravelDistance * _leverPosition - PlayerContactOverlap;

        if (playerBounds.Right <= contactX)
        {
            return;
        }

        float newPlayerX = contactX - playerBounds.Width;
        levelScene.Player.SetPosition(new Vector2(newPlayerX, levelScene.Player.Position.Y));
    }

    private void UpdateLeverPosition()
    {
        SetLocalPosition(WorldConfig.ThrottleIdleTopLeftLocal + new Vector2(WorldConfig.ThrottleTravelDistance * _leverPosition, 0f));
    }

    private void ResolvePlayerOverlapAfterMovement(Rectangle previousBounds)
    {
        if (_scene is not LevelScene levelScene)
        {
            return;
        }

        Rectangle currentBounds = GetBounds();
        if (currentBounds == Rectangle.Empty || previousBounds == Rectangle.Empty || currentBounds == previousBounds)
        {
            return;
        }

        Rectangle playerBounds = levelScene.PlayerBounds;
        if (!playerBounds.Intersects(currentBounds))
        {
            return;
        }

        Vector2 playerPosition = levelScene.Player.Position;

        if (currentBounds.Left < previousBounds.Left)
        {
            levelScene.Player.SetPosition(new Vector2(currentBounds.Left - playerBounds.Width, playerPosition.Y));
            return;
        }

        if (currentBounds.Left > previousBounds.Left)
        {
            levelScene.Player.SetPosition(new Vector2(currentBounds.Right, playerPosition.Y));
        }
    }

    private float GetIdleWorldX(LevelScene levelScene)
    {
        return levelScene.ResolvePosition(PositionSpace.Vehicle, WorldConfig.ThrottleIdleTopLeftLocal).X;
    }

    private Rectangle GetPushContactBounds()
    {
        Rectangle leverBounds = GetBounds();
        if (leverBounds == Rectangle.Empty)
        {
            return Rectangle.Empty;
        }

        return new Rectangle(
            leverBounds.Left - ContactPadding,
            leverBounds.Top - ContactPadding,
            ContactPadding + leverBounds.Width + 2,
            leverBounds.Height + ContactPadding * 2);
    }

    private Rectangle GetInteractionBounds()
    {
        if (_scene is not LevelScene levelScene)
        {
            return Rectangle.Empty;
        }

        int padding = WorldConfig.ThrottleInteractionPadding;
        Vector2 worldTopLeft = levelScene.ResolvePosition(
            PositionSpace.Vehicle,
            WorldConfig.ThrottleIdleTopLeftLocal - new Vector2(padding, padding));

        return new Rectangle(
            (int)System.MathF.Round(worldTopLeft.X),
            (int)System.MathF.Round(worldTopLeft.Y),
            WorldConfig.ThrottleSize.X + (int)WorldConfig.ThrottleTravelDistance + padding * 2,
            WorldConfig.ThrottleSize.Y + padding * 2);
    }

    private Color GetDamageTint()
    {
        if (!IsDamaged)
        {
            return Color.White;
        }

        int frame = (int)(_damageBlinkTimer / DamageBlinkInterval);
        return frame % 2 == 0 ? Color.Red : Color.White;
    }
}
