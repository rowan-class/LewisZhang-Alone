using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace LewisZhang_Alone;

public enum Action
{
    MoveLeft,
    MoveRight,
    Jump,
    StartGame,
    TogglePause,
    ToggleDebug,
    ToggleVehiclePower,
    ToggleCameraView,
    Interact,
    SaveGame,
    DebugSpeedDecrease,
    DebugSpeedIncrease,
    ToggleAutoPickupModule,
    ToggleSolarPanelModule,
    DebugRepairAll
}

public class Input
{
    private static readonly Dictionary<Action, List<Keys>> actionKeys =
        new Dictionary<Action, List<Keys>>()
        {
            { Action.MoveLeft,  new List<Keys> { Keys.Left, Keys.A } },
            { Action.MoveRight, new List<Keys> { Keys.Right, Keys.D } },
            { Action.Jump,      new List<Keys> { Keys.Space, Keys.W, Keys.Up } },
            { Action.StartGame, new List<Keys> { Keys.Enter } },
            { Action.TogglePause, new List<Keys> { Keys.Escape } },
            { Action.ToggleDebug, new List<Keys> { Keys.F3 } },
            { Action.ToggleVehiclePower, new List<Keys> { Keys.E } },
            { Action.ToggleCameraView, new List<Keys> { Keys.LeftShift, Keys.RightShift } },
            { Action.Interact, new List<Keys> { Keys.G } },
            { Action.SaveGame, new List<Keys> { Keys.F5 } },
            { Action.DebugSpeedDecrease, new List<Keys> { Keys.OemOpenBrackets } },
            { Action.DebugSpeedIncrease, new List<Keys> { Keys.OemCloseBrackets } },
            { Action.ToggleAutoPickupModule, new List<Keys> { Keys.D7, Keys.NumPad7 } },
            { Action.ToggleSolarPanelModule, new List<Keys> { Keys.D8, Keys.NumPad8 } },
            { Action.DebugRepairAll, new List<Keys> { Keys.F } }
        };

    private KeyboardState currentState;
    private KeyboardState previousState;

    public void Update()
    {
        previousState = currentState;
        currentState = Keyboard.GetState();
    }

    public bool IsActionPressed(Action action)
    {
        if (actionKeys.ContainsKey(action) == false)
            return false;

        foreach (Keys key in actionKeys[action])
        {
            if (IsKeyPressed(key))
                return true;
        }

        return false;
    }

    public bool IsKeyPressed(Keys key)
    {
        return currentState.IsKeyDown(key) && previousState.IsKeyUp(key);
    }

    public bool IsActionDown(Action action)
    {
        if (actionKeys.ContainsKey(action) == false)
        {
            return false;
        }

        foreach (Keys key in actionKeys[action])
        {
            if (IsKeyDown(key))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsKeyDown(Keys key)
    {
        return currentState.IsKeyDown(key);
    }
}
