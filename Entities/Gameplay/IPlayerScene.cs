using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public interface IPlayerScene
{
    bool IsPlayerCarryingItem { get; }
    bool IsPlayerInputLocked { get; }
    bool CanPlayerMoveLeft { get; }
    float VehicleSpeed { get; }
    Rectangle PlayerMovementBounds { get; }

    bool IsPlayerAttachedToVehicle(Player player);
    IEnumerable<Rectangle> GetSolidRectangles();
}
