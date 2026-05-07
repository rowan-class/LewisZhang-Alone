using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class WorldEventDefinition
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Target { get; set; }
    public List<DialogueLine> Dialogue { get; set; }
    public float TriggerDistance { get; set; }
    public float MinDistance { get; set; }
    public float MaxDistance { get; set; }
    public float ScrollSpeed { get; set; }
    public float Alpha { get; set; } = 0.7f;
}

internal enum SolarInstallStationPhase
{
    None = 0,
    Docking = 1,
    Ready = 2,
    Installing = 3,
    Completed = 4
}

internal class WorldEventRuntimeState
{
    public bool HasTriggered { get; set; }
    public bool IsActive { get; set; }
    public float RemainingDuration { get; set; }
    public float ScrollOffset { get; set; }
    public int SheetCount { get; set; }
    public int Phase { get; set; }
    public float AuxiliaryValue { get; set; }
}

public class WorldEventManager
{
    private const string SandstormType = "sandstorm";
    private const string SolarInstallStationType = "solar_install_station";
    private const string SolarDayType = "solar_day";
    private const string SolarNightType = "solar_night";
    private const string ComponentFailureType = "component_failure";
    private const string RepairGunDropType = "repair_gun_drop";
    private const string DialogueType = "dialogue";
    private const string ProjectFileName = "LewisZhang-Alone.csproj";
    private const int SandstormLayerCount = 3;
    private const float SandstormLeadInPadding = 120f;
    private const float SandstormSheetSpacingFactor = 0.72f;
    private const float SandstormLayerPhaseOffsetFactor = 0.28f;
    private const float SandstormBaseLayerSpeedStep = 0.08f;
    private const float SandstormLayerAlphaStep = 0.18f;
    private const int ButtonTriggerHeight = 12;
    private const int ButtonTriggerInsetX = 6;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly List<WorldEventDefinition> _definitions;
    private readonly Dictionary<string, WorldEventRuntimeState> _runtimeStates = new();
    private float _lastTravelDistance;

    public WorldEventManager()
    {
        _definitions = LoadDefinitions();
        foreach (WorldEventDefinition definition in _definitions)
        {
            if (!string.IsNullOrWhiteSpace(definition.Id))
            {
                _runtimeStates[definition.Id] = new WorldEventRuntimeState();
            }
        }
    }

    public void Update(GameTime gameTime, float travelDistance, Rectangle cameraViewBounds, LevelScene levelScene)
    {
        _lastTravelDistance = travelDistance;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        foreach (WorldEventDefinition definition in _definitions)
        {
            if (!_runtimeStates.TryGetValue(definition.Id, out WorldEventRuntimeState runtime))
            {
                continue;
            }

            if (!runtime.HasTriggered && travelDistance >= GetActivationDistance(definition))
            {
                Activate(definition, runtime, levelScene, cameraViewBounds);
            }

            if (!runtime.IsActive)
            {
                continue;
            }

            if (string.Equals(definition.Type, SandstormType, StringComparison.OrdinalIgnoreCase))
            {
                UpdateSandstorm(definition, runtime, dt, travelDistance, cameraViewBounds);
                continue;
            }

            if (string.Equals(definition.Type, SolarInstallStationType, StringComparison.OrdinalIgnoreCase))
            {
                UpdateSolarInstallStation(definition, runtime, dt, levelScene);
            }
        }

        UpdateSolarWeatherBlock(levelScene);
    }

    public void DrawWorld(SpriteBatch spriteBatch, Rectangle cameraViewBounds)
    {
        foreach (WorldEventDefinition definition in _definitions)
        {
            if (!_runtimeStates.TryGetValue(definition.Id, out WorldEventRuntimeState runtime) || !runtime.IsActive)
            {
                continue;
            }

            if (string.Equals(definition.Type, SolarInstallStationType, StringComparison.OrdinalIgnoreCase))
            {
                DrawSolarInstallStationWorld(spriteBatch, runtime, definition, cameraViewBounds);
            }
        }
    }

    public void DrawOverlay(SpriteBatch spriteBatch, Rectangle cameraViewBounds)
    {
        foreach (WorldEventDefinition definition in _definitions)
        {
            if (!_runtimeStates.TryGetValue(definition.Id, out WorldEventRuntimeState runtime) || !runtime.IsActive)
            {
                continue;
            }

            if (string.Equals(definition.Type, SandstormType, StringComparison.OrdinalIgnoreCase))
            {
                DrawSandstorm(spriteBatch, definition, runtime, cameraViewBounds);
                continue;
            }

            if (string.Equals(definition.Type, SolarInstallStationType, StringComparison.OrdinalIgnoreCase))
            {
                DrawSolarInstallStationOverlay(spriteBatch, runtime, definition);
            }
        }
    }

    public IEnumerable<Rectangle> GetSolidRectangles()
    {
        foreach (WorldEventDefinition definition in _definitions)
        {
            if (!_runtimeStates.TryGetValue(definition.Id, out WorldEventRuntimeState runtime) || !runtime.IsActive)
            {
                continue;
            }

            if (!string.Equals(definition.Type, SolarInstallStationType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!AreSolarInstallStationCollisionsActive(runtime))
            {
                continue;
            }

            foreach (Rectangle rect in GetSolarInstallStationCollisionWorldRectangles(definition))
            {
                yield return rect;
            }
        }
    }

    public bool IsThrottleLocked(float travelDistance)
    {
        foreach (WorldEventDefinition definition in _definitions)
        {
            if (!string.Equals(definition.Type, SolarInstallStationType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!_runtimeStates.TryGetValue(definition.Id, out WorldEventRuntimeState runtime))
            {
                continue;
            }

            if (!runtime.HasTriggered && travelDistance >= definition.TriggerDistance)
            {
                return true;
            }

            if (!runtime.IsActive)
            {
                continue;
            }

            SolarInstallStationPhase phase = (SolarInstallStationPhase)runtime.Phase;
            if (phase != SolarInstallStationPhase.Completed)
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<Rectangle> GetDebugRectangles()
    {
        foreach (WorldEventDefinition definition in _definitions)
        {
            if (!_runtimeStates.TryGetValue(definition.Id, out WorldEventRuntimeState runtime) || !runtime.IsActive)
            {
                continue;
            }

            if (!string.Equals(definition.Type, SolarInstallStationType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return GetSolarInstallStationWorldBounds(definition);

            if (AreSolarInstallStationCollisionsActive(runtime))
            {
                foreach (Rectangle rect in GetSolarInstallStationCollisionWorldRectangles(definition))
                {
                    yield return rect;
                }
            }

            if (ShouldShowSolarInstallButton(runtime))
            {
                yield return GetSolarInstallButtonWorldBounds(definition);
                if (CanPressSolarInstallButton(runtime))
                {
                    yield return GetSolarInstallButtonHeadHitBounds(definition);
                }
            }
        }
    }

    public string GetDebugStatus()
    {
        foreach (WorldEventDefinition definition in _definitions)
        {
            if (!_runtimeStates.TryGetValue(definition.Id, out WorldEventRuntimeState runtime) || !runtime.IsActive)
            {
                continue;
            }

            if (string.Equals(definition.Type, SolarInstallStationType, StringComparison.OrdinalIgnoreCase))
            {
                return GetSolarInstallStationDebugStatus(runtime);
            }

            if (string.Equals(definition.Type, SandstormType, StringComparison.OrdinalIgnoreCase))
            {
                return runtime.RemainingDuration > 0f
                    ? $"{definition.Type} {runtime.RemainingDuration:0.0} dist"
                    : $"{definition.Type} exiting";
            }
        }

        return "None";
    }

    public List<WorldEventSaveData> CaptureSaveData()
    {
        List<WorldEventSaveData> result = new();

        foreach ((string id, WorldEventRuntimeState runtime) in _runtimeStates)
        {
            result.Add(new WorldEventSaveData
            {
                Id = id,
                HasTriggered = runtime.HasTriggered,
                IsActive = runtime.IsActive,
                RemainingDuration = runtime.RemainingDuration,
                ScrollOffset = runtime.ScrollOffset,
                SheetCount = runtime.SheetCount,
                Phase = runtime.Phase,
                AuxiliaryValue = runtime.AuxiliaryValue
            });
        }

        return result;
    }

    public void RestoreSaveData(List<WorldEventSaveData> savedStates)
    {
        if (savedStates == null)
        {
            return;
        }

        foreach (WorldEventSaveData savedState in savedStates)
        {
            if (savedState?.Id == null || !_runtimeStates.TryGetValue(savedState.Id, out WorldEventRuntimeState runtime))
            {
                continue;
            }

            runtime.HasTriggered = savedState.HasTriggered;
            runtime.IsActive = savedState.IsActive;
            runtime.RemainingDuration = MathF.Max(0f, savedState.RemainingDuration);
            runtime.ScrollOffset = savedState.ScrollOffset;
            runtime.SheetCount = Math.Max(0, savedState.SheetCount);
            runtime.Phase = savedState.Phase;
            runtime.AuxiliaryValue = savedState.AuxiliaryValue;
        }
    }

    private void Activate(WorldEventDefinition definition, WorldEventRuntimeState runtime, LevelScene levelScene, Rectangle cameraViewBounds)
    {
        runtime.HasTriggered = true;
        runtime.IsActive = true;

        if (string.Equals(definition.Type, SandstormType, StringComparison.OrdinalIgnoreCase))
        {
            runtime.RemainingDuration = MathF.Max(0f, GetSandstormMaxDistance(definition) - GetSandstormMinDistance(definition));
            runtime.SheetCount = 1;
            runtime.ScrollOffset = 0f;
            runtime.Phase = 0;
            runtime.AuxiliaryValue = 0f;
            return;
        }

        if (string.Equals(definition.Type, SolarInstallStationType, StringComparison.OrdinalIgnoreCase))
        {
            runtime.RemainingDuration = 0f;
            runtime.ScrollOffset = 0f;
            runtime.SheetCount = 0;
            runtime.Phase = (int)SolarInstallStationPhase.Docking;
            runtime.AuxiliaryValue = 0f;
            return;
        }

        if (string.Equals(definition.Type, SolarDayType, StringComparison.OrdinalIgnoreCase))
        {
            SetSolarDaytime(true, levelScene);
            runtime.IsActive = false;
            return;
        }

        if (string.Equals(definition.Type, SolarNightType, StringComparison.OrdinalIgnoreCase))
        {
            SetSolarDaytime(false, levelScene);
            runtime.IsActive = false;
            return;
        }

        if (string.Equals(definition.Type, ComponentFailureType, StringComparison.OrdinalIgnoreCase))
        {
            levelScene?.DamageComponent(definition.Target);
            runtime.IsActive = false;
            return;
        }

        if (string.Equals(definition.Type, RepairGunDropType, StringComparison.OrdinalIgnoreCase))
        {
            float spawnScreenX = cameraViewBounds.Left + cameraViewBounds.Width * 0.75f;
            levelScene?.SpawnRepairGunDrop(_lastTravelDistance + spawnScreenX);
            runtime.IsActive = false;
            return;
        }

        if (string.Equals(definition.Type, DialogueType, StringComparison.OrdinalIgnoreCase))
        {
            levelScene?.StartDialogue(definition.Dialogue);
            runtime.IsActive = false;
        }
    }

    private static void SetSolarDaytime(bool isDaytime, LevelScene levelScene)
    {
        Game1.SetSolarPanelDaytime(isDaytime);
        if (!Game1.SolarPanelHasEnergy)
        {
            levelScene?.Vehicle.SetSolarDriveActive(false);
        }
    }

    private void UpdateSolarWeatherBlock(LevelScene levelScene)
    {
        bool isBlockedBySandstorm = IsSandstormActive();
        if (Game1.SolarPanelBlockedByWeather == isBlockedBySandstorm)
        {
            return;
        }

        Game1.SetSolarPanelWeatherBlocked(isBlockedBySandstorm);
        if (!Game1.SolarPanelHasEnergy)
        {
            levelScene?.Vehicle.SetSolarDriveActive(false);
        }
    }

    private bool IsSandstormActive()
    {
        foreach (WorldEventDefinition definition in _definitions)
        {
            if (!string.Equals(definition.Type, SandstormType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (_runtimeStates.TryGetValue(definition.Id, out WorldEventRuntimeState runtime) && runtime.IsActive)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateSandstorm(WorldEventDefinition definition, WorldEventRuntimeState runtime, float dt, float travelDistance, Rectangle cameraViewBounds)
    {
        runtime.ScrollOffset += definition.ScrollSpeed * dt;

        float maxDistance = GetSandstormMaxDistance(definition);
        if (travelDistance <= maxDistance)
        {
            float spacing = GetSandstormSheetSpacing(AssetManager.GetTexture(Art.Sandstorm).Width);
            runtime.SheetCount = Math.Max(1, 1 + (int)MathF.Floor(runtime.ScrollOffset / MathF.Max(1f, spacing)));
        }

        runtime.RemainingDuration = MathF.Max(0f, maxDistance - travelDistance);

        if (travelDistance > maxDistance && HasSandstormFullyExited(runtime, cameraViewBounds))
        {
            runtime.IsActive = false;
        }
    }

    private void UpdateSolarInstallStation(WorldEventDefinition definition, WorldEventRuntimeState runtime, float dt, LevelScene levelScene)
    {
        if (levelScene == null)
        {
            return;
        }

        SolarInstallStationPhase phase = (SolarInstallStationPhase)runtime.Phase;
        VehicleEntity vehicle = levelScene.Vehicle;

        switch (phase)
        {
            case SolarInstallStationPhase.Docking:
            {
                vehicle.ReleaseHandbrake();
                vehicle.SetPowered(false);
                vehicle.SetSolarDriveActive(false);

                float targetTravelDistance = GetSolarInstallStationDockedTravelDistance(definition);
                if (_lastTravelDistance < targetTravelDistance)
                {
                    float maxAllowedSpeed = dt > 0f
                        ? MathF.Max(0f, (targetTravelDistance - _lastTravelDistance) / dt)
                        : 0f;
                    float targetSpeed = MathF.Min(WorldConfig.SolarInstallStationAutoMoveSpeed, maxAllowedSpeed);
                    vehicle.SetBaseSpeed(MathF.Min(vehicle.BaseSpeed, maxAllowedSpeed));
                    vehicle.MoveBaseSpeedTowards(
                        targetSpeed,
                        WorldConfig.SolarInstallStationAutoMoveAcceleration,
                        dt);
                }
                else
                {
                    vehicle.SetBaseSpeed(0f);
                    runtime.Phase = (int)SolarInstallStationPhase.Ready;
                }

                break;
            }

            case SolarInstallStationPhase.Ready:
            {
                vehicle.SetPowered(false);
                vehicle.SetBaseSpeed(0f);
                if (Game1.SolarPanelEnabled)
                {
                    runtime.Phase = (int)SolarInstallStationPhase.Completed;
                    break;
                }

                if (CanPressSolarInstallButton(runtime) && IsSolarInstallButtonPressed(levelScene.Player, definition))
                {
                    runtime.Phase = (int)SolarInstallStationPhase.Installing;
                    runtime.AuxiliaryValue = GetSolarInstallAnimatedPanelStartWorldY(definition);
                }

                break;
            }

            case SolarInstallStationPhase.Installing:
            {
                vehicle.SetPowered(false);
                vehicle.SetBaseSpeed(0f);

                float targetY = levelScene.Vehicle.Position.Y + WorldConfig.SolarPanelTopLeftLocal.Y;
                runtime.AuxiliaryValue = MathF.Min(
                    targetY,
                    runtime.AuxiliaryValue + WorldConfig.SolarInstallPanelDropSpeed * dt);

                if (runtime.AuxiliaryValue >= targetY)
                {
                    runtime.AuxiliaryValue = targetY;
                    Game1.SolarPanelEnabled = true;
                    vehicle.SetSolarDriveActive(false);
                    runtime.Phase = (int)SolarInstallStationPhase.Completed;
                }

                break;
            }
        }
    }

    private void DrawSandstorm(SpriteBatch spriteBatch, WorldEventDefinition definition, WorldEventRuntimeState runtime, Rectangle cameraViewBounds)
    {
        Texture2D sandstormTexture = AssetManager.GetTexture(Art.Sandstorm);
        float textureWidth = sandstormTexture.Width;

        if (textureWidth <= 0f || runtime.SheetCount <= 0)
        {
            return;
        }

        float spacing = GetSandstormSheetSpacing(textureWidth);
        float firstSheetStartX = cameraViewBounds.Right + SandstormLeadInPadding;
        float alpha = MathHelper.Clamp(definition.Alpha, 0f, 1f);

        for (int layer = SandstormLayerCount - 1; layer >= 0; layer--)
        {
            float speedMultiplier = GetSandstormLayerSpeedMultiplier(layer);
            float phaseOffset = spacing * SandstormLayerPhaseOffsetFactor * layer;
            float layerAlphaMultiplier = MathF.Max(0.35f, 1f - SandstormLayerAlphaStep * layer);
            Color tint = Color.White * (alpha * layerAlphaMultiplier);

            for (int i = 0; i < runtime.SheetCount; i++)
            {
                float x = firstSheetStartX + phaseOffset + spacing * i - runtime.ScrollOffset * speedMultiplier;

                if (x + textureWidth < cameraViewBounds.Left - textureWidth)
                {
                    continue;
                }

                if (x > cameraViewBounds.Right + textureWidth)
                {
                    break;
                }

                Rectangle destination = new(
                    (int)MathF.Round(x),
                    0,
                    (int)MathF.Round(textureWidth),
                    WorldConfig.WorldHeight);

                spriteBatch.Draw(sandstormTexture, destination, tint);
            }
        }
    }

    private void DrawSolarInstallStationWorld(SpriteBatch spriteBatch, WorldEventRuntimeState runtime, WorldEventDefinition definition, Rectangle cameraViewBounds)
    {
        Rectangle bounds = GetSolarInstallStationWorldBounds(definition);
        if (bounds.Right < cameraViewBounds.Left - 64 || bounds.Left > cameraViewBounds.Right + 64)
        {
            return;
        }

        Texture2D pixel = AssetManager.GetTexture(Art.pixel);
        spriteBatch.Draw(pixel, bounds, new Color(255, 255, 255, 235));

        foreach (Rectangle platform in GetSolarInstallStationCollisionWorldRectangles(definition))
        {
            spriteBatch.Draw(pixel, platform, new Color(220, 220, 220));
        }

        if (ShouldShowSolarInstallButton(runtime))
        {
            Art art = runtime.Phase == (int)SolarInstallStationPhase.Installing
                ? Art.FuelButtonPressed
                : Art.FuelButtonIdle;
            spriteBatch.Draw(AssetManager.GetTexture(art), GetSolarInstallButtonWorldBounds(definition), Color.White);
        }
    }

    private void DrawSolarInstallStationOverlay(SpriteBatch spriteBatch, WorldEventRuntimeState runtime, WorldEventDefinition definition)
    {
        if ((SolarInstallStationPhase)runtime.Phase == SolarInstallStationPhase.Completed)
        {
            return;
        }

        float panelWorldY = runtime.Phase == (int)SolarInstallStationPhase.Installing
            ? runtime.AuxiliaryValue
            : GetSolarInstallAnimatedPanelStartWorldY(definition);
        Rectangle animatedPanelBounds = GetSolarInstallAnimatedPanelWorldBounds(panelWorldY);
        Art art = Game1.SolarPanelHasEnergy ? Art.SolarPanelPowered : Art.SolarPanelUnpowered;
        spriteBatch.Draw(AssetManager.GetTexture(art), animatedPanelBounds, Color.White);
    }

    private static float GetSandstormSheetSpacing(float textureWidth)
    {
        return MathF.Max(1f, textureWidth * SandstormSheetSpacingFactor);
    }

    private static float GetSandstormLayerSpeedMultiplier(int layerIndex)
    {
        return MathF.Max(0.7f, 1f - SandstormBaseLayerSpeedStep * layerIndex);
    }

    private bool HasSandstormFullyExited(WorldEventRuntimeState runtime, Rectangle cameraViewBounds)
    {
        Texture2D sandstormTexture = AssetManager.GetTexture(Art.Sandstorm);
        float textureWidth = sandstormTexture.Width;
        if (textureWidth <= 0f || runtime.SheetCount <= 0)
        {
            return true;
        }

        float spacing = GetSandstormSheetSpacing(textureWidth);
        float firstSheetStartX = cameraViewBounds.Right + SandstormLeadInPadding;
        float slowestLayerSpeedMultiplier = GetSandstormLayerSpeedMultiplier(SandstormLayerCount - 1);
        float slowestLayerPhaseOffset = spacing * SandstormLayerPhaseOffsetFactor * (SandstormLayerCount - 1);
        float tailX = firstSheetStartX
            + slowestLayerPhaseOffset
            + spacing * (runtime.SheetCount - 1)
            - runtime.ScrollOffset * slowestLayerSpeedMultiplier;

        return tailX + textureWidth < cameraViewBounds.Left;
    }

    private static float GetActivationDistance(WorldEventDefinition definition)
    {
        return string.Equals(definition.Type, SandstormType, StringComparison.OrdinalIgnoreCase)
            ? GetSandstormMinDistance(definition)
            : definition.TriggerDistance;
    }

    private static float GetSandstormMinDistance(WorldEventDefinition definition)
    {
        return MathF.Max(0f, definition.MinDistance);
    }

    private static float GetSandstormMaxDistance(WorldEventDefinition definition)
    {
        return MathF.Max(GetSandstormMinDistance(definition), definition.MaxDistance);
    }

    private static bool ShouldShowSolarInstallButton(WorldEventRuntimeState runtime)
    {
        return runtime.Phase == (int)SolarInstallStationPhase.Docking
            || runtime.Phase == (int)SolarInstallStationPhase.Ready
            || runtime.Phase == (int)SolarInstallStationPhase.Installing
            || runtime.Phase == (int)SolarInstallStationPhase.Completed;
    }

    private static bool CanPressSolarInstallButton(WorldEventRuntimeState runtime)
    {
        return !Game1.SolarPanelEnabled && runtime.Phase == (int)SolarInstallStationPhase.Ready;
    }

    private static bool AreSolarInstallStationCollisionsActive(WorldEventRuntimeState runtime)
    {
        return runtime.Phase != (int)SolarInstallStationPhase.Docking;
    }

    private string GetSolarInstallStationDebugStatus(WorldEventRuntimeState runtime)
    {
        return ((SolarInstallStationPhase)runtime.Phase) switch
        {
            SolarInstallStationPhase.Docking => "solar_install_station docking",
            SolarInstallStationPhase.Ready => "solar_install_station ready",
            SolarInstallStationPhase.Installing => "solar_install_station installing",
            SolarInstallStationPhase.Completed => "solar_install_station completed",
            _ => "solar_install_station"
        };
    }

    private bool IsSolarInstallButtonPressed(Player player, WorldEventDefinition definition)
    {
        Rectangle headRect = GetPlayerHeadBounds(player);
        return player.Velocity.Y < 0f
            && headRect != Rectangle.Empty
            && headRect.Intersects(GetSolarInstallButtonHeadHitBounds(definition));
    }

    private static Rectangle GetPlayerHeadBounds(Player player)
    {
        Rectangle playerBounds = player.GetBounds();
        if (playerBounds == Rectangle.Empty)
        {
            return Rectangle.Empty;
        }

        int width = Math.Max(1, playerBounds.Width - 16);
        return new Rectangle(
            playerBounds.X + 8,
            playerBounds.Y,
            width,
            Math.Max(1, playerBounds.Height / 4));
    }

    private Rectangle GetSolarInstallStationWorldBounds(WorldEventDefinition definition)
    {
        int x = (int)MathF.Round(GetSolarInstallStationBaseWorldLocalX(definition) - _lastTravelDistance);
        int y = WorldConfig.FakeGroundLocalRect.Y - WorldConfig.SolarInstallStationSize.Y;
        return new Rectangle(x, y, WorldConfig.SolarInstallStationSize.X, WorldConfig.SolarInstallStationSize.Y);
    }

    private IEnumerable<Rectangle> GetSolarInstallStationCollisionWorldRectangles(WorldEventDefinition definition)
    {
        Rectangle stationBounds = GetSolarInstallStationWorldBounds(definition);
        foreach (Rectangle localRect in WorldConfig.SolarInstallStationCollisionLocalRectangles)
        {
            yield return new Rectangle(
                stationBounds.X + localRect.X,
                stationBounds.Y + localRect.Y,
                localRect.Width,
                localRect.Height);
        }
    }

    private Rectangle GetSolarInstallButtonWorldBounds(WorldEventDefinition definition)
    {
        Rectangle stationBounds = GetSolarInstallStationWorldBounds(definition);
        return new Rectangle(
            stationBounds.X + (int)MathF.Round(WorldConfig.SolarInstallStationButtonTopLeftLocal.X),
            stationBounds.Y + (int)MathF.Round(WorldConfig.SolarInstallStationButtonTopLeftLocal.Y),
            WorldConfig.FuelButtonSize.X,
            WorldConfig.FuelButtonSize.Y);
    }

    private Rectangle GetSolarInstallButtonHeadHitBounds(WorldEventDefinition definition)
    {
        Rectangle bounds = GetSolarInstallButtonWorldBounds(definition);
        return new Rectangle(
            bounds.X + ButtonTriggerInsetX,
            bounds.Bottom,
            Math.Max(1, bounds.Width - ButtonTriggerInsetX * 2),
            ButtonTriggerHeight);
    }

    private Rectangle GetSolarInstallAnimatedPanelWorldBounds(float panelWorldY)
    {
        int x = (int)MathF.Round(WorldConfig.VehiclePosition.X + WorldConfig.SolarPanelTopLeftLocal.X);
        int y = (int)MathF.Round(panelWorldY);
        return new Rectangle(x, y, WorldConfig.SolarPanelSize.X, WorldConfig.SolarPanelSize.Y);
    }

    private float GetSolarInstallAnimatedPanelStartWorldY(WorldEventDefinition definition)
    {
        Rectangle stationBounds = GetSolarInstallStationWorldBounds(definition);
        return stationBounds.Y + WorldConfig.SolarInstallAnimatedPanelTopLeftLocal.Y;
    }

    private static float GetSolarInstallStationBaseWorldLocalX(WorldEventDefinition definition)
    {
        return definition.TriggerDistance + WorldConfig.SolarInstallStationStartScreenX;
    }

    private static float GetSolarInstallStationDockedTravelDistance(WorldEventDefinition definition)
    {
        return GetSolarInstallStationBaseWorldLocalX(definition) - WorldConfig.SolarInstallStationDockedScreenX;
    }

    private static List<WorldEventDefinition> LoadDefinitions()
    {
        string path = GetEventConfigPath();
        if (!File.Exists(path))
        {
            return new List<WorldEventDefinition>();
        }

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<WorldEventDefinition>>(json, JsonOptions) ?? new List<WorldEventDefinition>();
    }

    private static string GetEventConfigPath()
    {
        DirectoryInfo directory = new(Environment.CurrentDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ProjectFileName)))
            {
                return Path.Combine(directory.FullName, "data", "events.json");
            }

            directory = directory.Parent;
        }

        return Path.Combine(Environment.CurrentDirectory, "data", "events.json");
    }
}
