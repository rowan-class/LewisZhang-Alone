using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public sealed class IntroToLevelTransition
{
    public Vector2 PlayerVehicleLocalPosition { get; init; }
    public bool IsCarryingFuelBarrel { get; init; }
}

public sealed class LevelToFinalWalkTransition
{
    public Vector2 PlayerVehicleLocalPosition { get; init; }
    public float ModuleDamageBlinkTimer { get; init; }
    public float FuelRatio { get; init; }
    public bool FuelPortLit { get; init; }
    public bool FuelPortDamaged { get; init; }
    public bool SolarPanelInstalled { get; init; }
    public bool SolarPanelDamaged { get; init; }
    public bool ThrottleDamaged { get; init; }
    public bool AutoPickupDamaged { get; init; }
    public bool HandbrakeActive { get; init; }
    public bool SolarDriveActive { get; init; }
}

public static class SceneTransitionContext
{
    public static IntroToLevelTransition IntroToLevel { get; set; }
    public static LevelToFinalWalkTransition LevelToFinalWalk { get; set; }

    public static IntroToLevelTransition ConsumeIntroToLevel()
    {
        IntroToLevelTransition transition = IntroToLevel;
        IntroToLevel = null;
        return transition;
    }

    public static LevelToFinalWalkTransition ConsumeLevelToFinalWalk()
    {
        LevelToFinalWalkTransition transition = LevelToFinalWalk;
        LevelToFinalWalk = null;
        return transition;
    }
}
