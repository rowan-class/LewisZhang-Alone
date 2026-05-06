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
    public static readonly Point AutoPickupModuleSize = new(64, 64);
    public static readonly Point SolarPanelSize = new(200, 48);
    public static readonly Point SolarInstallStationSize = new(1480, 980);
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
    public static readonly Vector2 SolarButtonTopLeftLocal = new(600f, 84f);
    public static readonly Vector2 AutoPickupModuleTopLeftLocal = new(820f, 358f);
    public static readonly Vector2 AutoPickupStoredBarrelTopLeftLocal = new(780f, 386f);
    public static readonly Vector2 SolarPanelTopLeftLocal = new(370f, 16f);
    public static readonly Vector2 ThrottleIdleTopLeftLocal = new(760f, 168f);
    public static readonly Vector2 SolarInstallStationButtonTopLeftLocal = new(1200f, 186f);
    public static readonly Vector2 SolarInstallAnimatedPanelTopLeftLocal = new(980f, -120f);
    public const float FuelButtonPressedDuration = 0.35f;
    public const float FuelButtonCooldownDuration = 0.65f;
    public const float ThrottleTravelDistance = 48f;
    public const int ThrottleInteractionPadding = 20;
    public const float SolarInstallStationStartScreenX = 3260f;
    public const float SolarInstallStationDockedScreenX = 1760f;
    public const float SolarInstallStationAutoMoveSpeed = 110f;
    public const float SolarInstallStationAutoMoveAcceleration = 180f;
    public const float SolarInstallPanelDropSpeed = 240f;
    public static readonly Rectangle CloseViewBounds = new((int)(VehiclePosition.X - 140f), (int)(VehiclePosition.Y - 120f), ScreenWidth, ScreenHeight);
    public static readonly Rectangle VehicleCabinBoundsLocal = new(60, -120, 860, 540);
    public static readonly Rectangle FakeGroundLocalRect = new(-20000, OverviewViewBounds.Bottom - 120, 50000, 120);
    public static readonly Rectangle[] SolarInstallStationCollisionLocalRectangles =
    {
        //天花板
        new Rectangle(1100, 186-24, 200, 24),
        //最上一层
        new Rectangle(100, 300, 1200, 24),
        //楼梯
        new Rectangle(0, 800, 250, 24),
        new Rectangle(100, 700, 200, 24),
        new Rectangle(0, 600, 160, 24),
        new Rectangle(100, 500, 160, 24),
        new Rectangle(0, 400, 160, 24),
        //左墙壁
        new Rectangle(0, 0, 24, 980),
        //右墙壁
        new Rectangle(1456, 0, 24, 100),
    };
}
