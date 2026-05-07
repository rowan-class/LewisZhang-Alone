using System;
using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class Camera2D
{
    private readonly Random _random = new(17);

    private Rectangle _baseViewBounds = WorldConfig.OverviewViewBounds;
    private float _maxLookAheadX = WorldConfig.OverviewCameraMaxLookAheadX;
    private float _lookAheadX;
    private Vector2 _shakeOffset;
    private float _shakeTimer;
    private float _nextShakeTimer = WorldConfig.CameraShakeMaxDelay;
    private float _activeShakeStrength;

    public Rectangle ViewBounds => new(
        (int)MathF.Round(_baseViewBounds.X + _lookAheadX + _shakeOffset.X),
        (int)MathF.Round(_baseViewBounds.Y + _shakeOffset.Y),
        _baseViewBounds.Width,
        _baseViewBounds.Height);

    public void ShowOverview()
    {
        _baseViewBounds = WorldConfig.OverviewViewBounds;
        _maxLookAheadX = WorldConfig.OverviewCameraMaxLookAheadX;
    }

    public void ShowCloseVehicleView()
    {
        _baseViewBounds = WorldConfig.CloseViewBounds;
        _maxLookAheadX = WorldConfig.CloseCameraMaxLookAheadX;
    }

    public void Update(GameTime gameTime, float vehicleSpeed)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (dt <= 0f)
        {
            return;
        }

        float speedAmount = MathHelper.Clamp(vehicleSpeed / WorldConfig.VehicleMaxSpeed, 0f, 1f);
        float targetLookAheadX = speedAmount * _maxLookAheadX;
        float smoothing = 1f - MathF.Exp(-WorldConfig.CameraLookAheadSharpness * dt);

        _lookAheadX = MathHelper.Lerp(_lookAheadX, targetLookAheadX, smoothing);
        UpdateShake(dt, speedAmount, vehicleSpeed);
    }

    public Matrix GetViewMatrix()
    {
        float scaleX = (float)WorldConfig.ScreenWidth / _baseViewBounds.Width;
        float scaleY = (float)WorldConfig.ScreenHeight / _baseViewBounds.Height;
        float cameraX = _baseViewBounds.X + _lookAheadX + _shakeOffset.X;
        float cameraY = _baseViewBounds.Y + _shakeOffset.Y;

        return Matrix.CreateTranslation(-cameraX, -cameraY, 0f)
            * Matrix.CreateScale(scaleX, scaleY, 1f);
    }

    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        float scaleX = (float)WorldConfig.ScreenWidth / _baseViewBounds.Width;
        float scaleY = (float)WorldConfig.ScreenHeight / _baseViewBounds.Height;
        float cameraX = _baseViewBounds.X + _lookAheadX + _shakeOffset.X;
        float cameraY = _baseViewBounds.Y + _shakeOffset.Y;

        return new Vector2(
            (worldPosition.X - cameraX) * scaleX,
            (worldPosition.Y - cameraY) * scaleY);
    }

    public Rectangle WorldToScreen(Rectangle worldBounds)
    {
        float scaleX = (float)WorldConfig.ScreenWidth / _baseViewBounds.Width;
        float scaleY = (float)WorldConfig.ScreenHeight / _baseViewBounds.Height;
        Vector2 screenTopLeft = WorldToScreen(new Vector2(worldBounds.X, worldBounds.Y));

        return new Rectangle(
            (int)MathF.Round(screenTopLeft.X),
            (int)MathF.Round(screenTopLeft.Y),
            (int)MathF.Round(worldBounds.Width * scaleX),
            (int)MathF.Round(worldBounds.Height * scaleY));
    }

    private void UpdateShake(float dt, float speedAmount, float vehicleSpeed)
    {
        if (vehicleSpeed < WorldConfig.CameraMinShakeSpeed)
        {
            _shakeTimer = 0f;
            _nextShakeTimer = WorldConfig.CameraShakeMaxDelay;
            _shakeOffset = Vector2.Zero;
            return;
        }

        if (_shakeTimer > 0f)
        {
            _shakeTimer -= dt;

            if (_shakeTimer <= 0f)
            {
                _shakeOffset = Vector2.Zero;
                _nextShakeTimer = RandomRange(WorldConfig.CameraShakeMinDelay, WorldConfig.CameraShakeMaxDelay);
                return;
            }

            _shakeOffset = new Vector2(
                RandomRange(-_activeShakeStrength, _activeShakeStrength),
                RandomRange(-_activeShakeStrength, _activeShakeStrength));
            return;
        }

        _nextShakeTimer -= dt;
        if (_nextShakeTimer > 0f)
        {
            return;
        }

        _activeShakeStrength = WorldConfig.CameraShakeMaxOffset * speedAmount;
        _shakeTimer = RandomRange(WorldConfig.CameraShakeMinDuration, WorldConfig.CameraShakeMaxDuration);
    }

    private float RandomRange(float min, float max)
    {
        return min + (float)_random.NextDouble() * (max - min);
    }
}
