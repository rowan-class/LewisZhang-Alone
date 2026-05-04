using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public static class WorldConfig
{
    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;
    public const int WorldWidth = 5120;
    public const int WorldHeight = 1600;
    public const int OverviewWidth = 2560;
    public const int OverviewHeight = 1440;
    public const float VehicleMaxSpeed = 200f;
    public const float OverviewCameraMaxLookAheadX = 560f;
    public const float CloseCameraMaxLookAheadX = 120f;
    public const float CameraLookAheadSharpness = 8f;
    public const float CameraMinShakeSpeed = 20f;
    public const float CameraShakeMaxOffset = 2f;
    public const float CameraShakeMinDelay = 2f;
    public const float CameraShakeMaxDelay = 5f;
    public const float CameraShakeMinDuration = 1f;
    public const float CameraShakeMaxDuration = 2f;

    public static readonly Vector2 ScreenSize = new(ScreenWidth, ScreenHeight);
    public static readonly Point PlayerSize = new(48, 48);
    public static readonly Point VehicleSize = new(1000, 540);
    public static readonly Point FuelBarrelSize = new(36, 36);
    public static readonly Point FuelDisplaySize = new(62, 210);
    public static readonly Point FuelPortSize = new(64, 64);
    public static readonly Point FuelButtonSize = new(48, 24);
    public static readonly Point ThrottleSize = new(12, 36);

    public static readonly Rectangle OverviewViewBounds = new(
        (WorldWidth - OverviewWidth) / 2,
        (WorldHeight - OverviewHeight) / 2,
        OverviewWidth,
        OverviewHeight);

    public static readonly Vector2 VehiclePosition = new(
        OverviewViewBounds.X + (OverviewViewBounds.Width - VehicleSize.X) / 2f,
        OverviewViewBounds.Bottom - VehicleSize.Y - 120f);

    public static readonly Vector2 PlayerStartPosition = VehiclePosition + new Vector2(520f, 278f);
    public static readonly Vector2 FuelDisplayBottomLeftLocal = new(80-10f, 300+20f);
    public static readonly Vector2 FuelPortTopLeftLocal = new(142f, 262f);
    public static readonly Vector2 FuelButtonTopLeftLocal = new(300f, 216f);
    public static readonly Vector2 HandbrakeButtonTopLeftLocal = new(680f, 84f);
    public static readonly Vector2 ThrottleIdleTopLeftLocal = new(760f, 168f);
    public const float FuelButtonPressedDuration = 0.35f;
    public const float FuelButtonCooldownDuration = 0.65f;
    public const float ThrottleTravelDistance = 48f;
    public const int ThrottleInteractionPadding = 20;
    public static readonly Rectangle CloseViewBounds = new((int)(VehiclePosition.X - 140f), (int)(VehiclePosition.Y - 120f), ScreenWidth, ScreenHeight);
    public static readonly Rectangle VehicleCabinBoundsLocal = new(60, -120, 860, 540);
    public static readonly Rectangle FakeGroundLocalRect = new(-20000, OverviewViewBounds.Bottom - 120, 50000, 120);
}
