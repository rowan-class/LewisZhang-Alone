using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LewisZhang_Alone;

public enum Action
{
    MoveLeft,
    MoveRight,
    Jump
}

public class Input
{
    private static Dictionary<Action, List<Keys>> actionKeys =
        new Dictionary<Action, List<Keys>>()
        {
            { Action.MoveLeft,  new List<Keys> { Keys.Left, Keys.A } },
            { Action.MoveRight, new List<Keys> { Keys.Right, Keys.D } },
            { Action.Jump,      new List<Keys> { Keys.Space } }
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