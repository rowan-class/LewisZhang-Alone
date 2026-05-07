using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public sealed class IntroToLevelTransition
{
    public Vector2 PlayerVehicleLocalPosition { get; init; }
}

public sealed class LevelToFinalWalkTransition
{
    public Vector2 PlayerVehicleLocalPosition { get; init; }
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
