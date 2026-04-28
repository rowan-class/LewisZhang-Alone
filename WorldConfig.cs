using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public static class WorldConfig
{
    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;
    public const int WorldWidth = 2560;
    public const int WorldHeight = 1440;

    public static readonly Vector2 ScreenSize = new(ScreenWidth, ScreenHeight);
    public static readonly Point PlayerSize = new(64, 64);
    public static readonly Point VehicleSize = new(1000, 540);
    public static readonly Point FuelBarrelSize = new(40, 40);

    public static readonly Vector2 VehiclePosition = new((WorldWidth - VehicleSize.X) / 2f, WorldHeight - VehicleSize.Y - 120f);
    public static readonly Vector2 PlayerStartPosition = VehiclePosition + new Vector2(140f, 300f);
    public static readonly Rectangle CloseViewBounds = new(640, 720, 1280, 720);
    public static readonly Rectangle VehicleCabinBoundsLocal = new(60, 90, 860, 320);
    public static readonly Rectangle FakeGroundLocalRect = new(-20000, WorldHeight - 120, 50000, 120);
}
