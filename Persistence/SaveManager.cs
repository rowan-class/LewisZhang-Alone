using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace LewisZhang_Alone;

public class SaveData
{
    public float WorldScrollX { get; set; }
    public float NextFuelSpawnX { get; set; }
    public bool PreferOverviewView { get; set; }
    public VectorSaveData PlayerPosition { get; set; }
    public VehicleSaveData Vehicle { get; set; }
    public ThrottleSaveData Throttle { get; set; }
    public bool FuelPortLit { get; set; }
    public List<FuelBarrelSaveData> FuelBarrels { get; set; } = new();
    public int CarriedFuelBarrelIndex { get; set; } = -1;
}

public class VectorSaveData
{
    public float X { get; set; }
    public float Y { get; set; }

    public static VectorSaveData FromVector2(Vector2 value)
    {
        return new VectorSaveData { X = value.X, Y = value.Y };
    }

    public Vector2 ToVector2()
    {
        return new Vector2(X, Y);
    }
}

public class VehicleSaveData
{
    public float Speed { get; set; }
    public float Fuel { get; set; }
    public bool Powered { get; set; }
}

public class ThrottleSaveData
{
    public ThrottleState State { get; set; }
    public float LeverPosition { get; set; }
    public float HoldTimer { get; set; }
}

public class FuelBarrelSaveData
{
    public PositionSpace Space { get; set; }
    public VectorSaveData LocalPosition { get; set; }
}

public static class SaveManager
{
    private const string SaveFileName = "save.json";
    private const string ProjectFileName = "LewisZhang-Alone.csproj";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string SavePath => Path.Combine(GetSaveDirectory(), SaveFileName);

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static void Save(SaveData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
        File.WriteAllText(SavePath, JsonSerializer.Serialize(data, JsonOptions));
    }

    public static SaveData Load()
    {
        if (!HasSave())
        {
            return null;
        }

        return JsonSerializer.Deserialize<SaveData>(File.ReadAllText(SavePath), JsonOptions);
    }

    public static void DeleteSave()
    {
        if (HasSave())
        {
            File.Delete(SavePath);
        }
    }

    private static string GetSaveDirectory()
    {
        DirectoryInfo directory = new(Environment.CurrentDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ProjectFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }
}
