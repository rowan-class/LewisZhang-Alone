
using System;
using System.Collections.Generic;

namespace LewisZhang_Alone;

public class GameState
{
    public const string HpKey = "hp";
    public const int MaxHp = 5;

    private Dictionary<string, int> stateData;

    public GameState()
    {
        stateData = new Dictionary<string, int>();
    }

    public int GetHp()
    {
        return GetState(HpKey);
    }

    public void SetHp(int hp)
    {
        SetState(HpKey, Math.Max(0, hp));
    }

    public void ResetHp()
    {
        SetHp(MaxHp);
    }

    public void DamageHp(int damage)
    {
        if (damage <= 0)
        {
            return;
        }
        SetHp(GetHp() - damage);
    }

    public void SetState(string key, int value)
    {
        stateData[key] = value;
    }

    public void AddToState(string key, int value)
    {
        if (stateData.ContainsKey(key) == false)
        {
            SetState(key, value);
        }
        else
        {
            stateData[key] += value;
        }
    }

    public int GetState(string key)
    {
        if (stateData.ContainsKey(key) == false)
        {
            return 0;
        }
        return stateData[key];
    }
}
