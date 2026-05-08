using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public readonly record struct RockObstacle(
    Art Art,
    float WorldLocalX,
    Point Size);
