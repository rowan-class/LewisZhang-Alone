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
    public float Duration { get; set; }
    public float MinDistance { get; set; }
    public float MaxDistance { get; set; }
    public float ScrollSpeed { get; set; }
    public float Alpha { get; set; } = 0.7f;
    public bool LockPlayer { get; set; }
    public float MinInterval { get; set; }
    public float MaxInterval { get; set; }
    public float FlashDuration { get; set; }
    public int BoltCount { get; set; }
}

internal enum SolarInstallStationPhase
{
    None = 0,
    Docking = 1,
    Ready = 2,
    Installing = 3,
    Completed = 4,
    Approaching = 5
}

internal enum ReturnCapsulePhase
{
    None = 0,
    Ready = 1,
    Launching = 2,
    Completed = 3
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
    private const string LightningType = "lightning";
    private const string SolarInstallStationType = "solar_install_station";
    private const string SolarDayType = "solar_day";
    private const string SolarNightType = "solar_night";
    private const string ComponentFailureType = "component_failure";
    private const string RepairGunDropType = "repair_gun_drop";
    private const string DialogueType = "dialogue";
    private const string ReturnCapsuleType = "return_capsule";
    private const string VehicleReachedType = "vehicle_reached";
    private const string FinalWalkType = "final_walk";
    private const string ProjectFileName = "LewisZhang-Alone.csproj";
    private const int SandstormLayerCount = 3;
    private const float SandstormLeadInPadding = 120f;
    private const float SandstormSheetSpacingFactor = 0.72f;
    private const float SandstormLayerPhaseOffsetFactor = 0.28f;
    private const float SandstormBaseLayerSpeedStep = 0.08f;
    private const float SandstormLayerAlphaStep = 0.18f;
    private const float LightningDefaultMinInterval = 0.6f;
    private const float LightningDefaultMaxInterval = 2.4f;
    private const float LightningDefaultFlashDuration = 0.12f;
    private const int LightningDefaultBoltCount = 2;
    private const int LightningSegmentCount = 9;
    private const float LightningBranchChance = 0.45f;
    private const float LightningBoltDelaySpread = 0.58f;
    private const float LightningBoltMinVisibleWindow = 0.22f;
    private const float LightningBoltMaxVisibleWindow = 0.42f;
    private const float SolarInstallStationLeadInDistance = 1600f;
    private const float SolarInstallStationDockingSnapDistance = 8f;
    private const float RepairGunDropSpawnPadding = 160f;
    private const int ButtonTriggerHeight = 12;
    private const int ButtonTriggerInsetX = 6;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly List<WorldEventDefinition> _definitions;
    private readonly Dictionary<string, WorldEventRuntimeState> _runtimeStates = new();
    private readonly Random _random = new(17);
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

            if (string.Equals(definition.Type, LightningType, StringComparison.OrdinalIgnoreCase))
            {
                UpdateLightning(definition, runtime, dt, travelDistance);
                continue;
            }

            if (string.Equals(definition.Type, SolarInstallStationType, StringComparison.OrdinalIgnoreCase))
            {
                UpdateSolarInstallStation(definition, runtime, dt, levelScene);
                continue;
            }

            if (string.Equals(definition.Type, ReturnCapsuleType, StringComparison.OrdinalIgnoreCase))
            {
                UpdateReturnCapsule(definition, runtime, dt, cameraViewBounds, levelScene);
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
                continue;
            }

            if (string.Equals(definition.Type, ReturnCapsuleType, StringComparison.OrdinalIgnoreCase))
            {
                DrawReturnCapsuleWorld(spriteBatch, runtime, definition, cameraViewBounds);
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

            if (string.Equals(definition.Type, LightningType, StringComparison.OrdinalIgnoreCase))
            {
                DrawLightning(spriteBatch, definition, runtime, cameraViewBounds);
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

            if (string.Equals(definition.Type, SolarInstallStationType, StringComparison.OrdinalIgnoreCase))
            {
                if (!AreSolarInstallStationCollisionsActive(definition, runtime))
                {
                    continue;
                }

                foreach (Rectangle rect in GetSolarInstallStationCollisionWorldRectangles(definition))
                {
                    yield return rect;
                }

                continue;
            }

            if (string.Equals(definition.Type, ReturnCapsuleType, StringComparison.OrdinalIgnoreCase)
                && (ReturnCapsulePhase)runtime.Phase != ReturnCapsulePhase.Completed)
            {
                yield return GetReturnCapsuleLowerCollisionWorldBounds(definition);
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
            if (phase == SolarInstallStationPhase.Docking
                || phase == SolarInstallStationPhase.Ready
                || phase == SolarInstallStationPhase.Installing)
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

            if (string.Equals(definition.Type, SolarInstallStationType, StringComparison.OrdinalIgnoreCase))
            {
                yield return GetSolarInstallStationWorldBounds(definition);

                if (AreSolarInstallStationCollisionsActive(definition, runtime))
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

                continue;
            }

            if (string.Equals(definition.Type, ReturnCapsuleType, StringComparison.OrdinalIgnoreCase))
            {
                yield return GetReturnCapsuleLowerCollisionWorldBounds(definition);
                yield return GetReturnCapsuleEntranceWorldBounds(definition, runtime);
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

            if (string.Equals(definition.Type, LightningType, StringComparison.OrdinalIgnoreCase))
            {
                return runtime.RemainingDuration > 0f
                    ? $"{definition.Type} {runtime.RemainingDuration:0.0} dist"
                    : $"{definition.Type} ending";
            }

            if (string.Equals(definition.Type, ReturnCapsuleType, StringComparison.OrdinalIgnoreCase))
            {
                return GetReturnCapsuleDebugStatus(runtime);
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
            runtime.RemainingDuration = definition.Duration > 0f
                ? definition.Duration
                : MathF.Max(0f, GetSandstormMaxDistance(definition) - GetSandstormMinDistance(definition));
            runtime.SheetCount = 1;
            runtime.ScrollOffset = 0f;
            runtime.Phase = 0;
            runtime.AuxiliaryValue = 0f;
            return;
        }

        if (string.Equals(definition.Type, LightningType, StringComparison.OrdinalIgnoreCase))
        {
            runtime.RemainingDuration = MathF.Max(0f, GetWeatherMaxDistance(definition) - GetWeatherMinDistance(definition));
            runtime.SheetCount = 0;
            runtime.ScrollOffset = 0f;
            runtime.Phase = _random.Next(1, int.MaxValue);
            runtime.AuxiliaryValue = GetLightningFlashDuration(definition);
            return;
        }

        if (string.Equals(definition.Type, SolarInstallStationType, StringComparison.OrdinalIgnoreCase))
        {
            runtime.RemainingDuration = 0f;
            runtime.ScrollOffset = 0f;
            runtime.SheetCount = 0;
            runtime.Phase = (int)SolarInstallStationPhase.Approaching;
            runtime.AuxiliaryValue = 0f;
            return;
        }

        if (string.Equals(definition.Type, ReturnCapsuleType, StringComparison.OrdinalIgnoreCase))
        {
            runtime.RemainingDuration = 0f;
            runtime.ScrollOffset = 0f;
            runtime.SheetCount = 0;
            runtime.Phase = (int)ReturnCapsulePhase.Ready;
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
            float spawnScreenX = cameraViewBounds.Right + RepairGunDropSpawnPadding;
            levelScene?.SpawnRepairGunDrop(levelScene.WorldScrollX + spawnScreenX);
            runtime.IsActive = false;
            return;
        }

        if (string.Equals(definition.Type, DialogueType, StringComparison.OrdinalIgnoreCase))
        {
            levelScene?.StartDialogue(definition.Dialogue);
            runtime.IsActive = false;
            return;
        }

        if (string.Equals(definition.Type, FinalWalkType, StringComparison.OrdinalIgnoreCase))
        {
            levelScene?.BeginFinalWalkTransition();
            runtime.IsActive = false;
            return;
        }

        if (string.Equals(definition.Type, VehicleReachedType, StringComparison.OrdinalIgnoreCase))
        {
            levelScene?.ReachVehicle();
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

    public bool IsPlayerMovementLocked()
    {
        foreach (WorldEventDefinition definition in _definitions)
        {
            if (!definition.LockPlayer)
            {
                continue;
            }

            if (!_runtimeStates.TryGetValue(definition.Id, out WorldEventRuntimeState runtime) || !runtime.IsActive)
            {
                continue;
            }

            if (runtime.RemainingDuration > 0f)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateSandstorm(WorldEventDefinition definition, WorldEventRuntimeState runtime, float dt, float travelDistance, Rectangle cameraViewBounds)
    {
        runtime.ScrollOffset += definition.ScrollSpeed * dt;

        if (definition.Duration > 0f)
        {
            runtime.RemainingDuration = MathF.Max(0f, runtime.RemainingDuration - dt);
            if (runtime.RemainingDuration > 0f)
            {
                float spacing = GetSandstormSheetSpacing(AssetManager.GetTexture(Art.Sandstorm).Width);
                runtime.SheetCount = Math.Max(1, 1 + (int)MathF.Floor(runtime.ScrollOffset / MathF.Max(1f, spacing)));
            }

            if (runtime.RemainingDuration <= 0f && HasSandstormFullyExited(runtime, cameraViewBounds))
            {
                runtime.IsActive = false;
            }

            return;
        }

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

    private void UpdateLightning(WorldEventDefinition definition, WorldEventRuntimeState runtime, float dt, float travelDistance)
    {
        float maxDistance = GetWeatherMaxDistance(definition);
        runtime.RemainingDuration = MathF.Max(0f, maxDistance - travelDistance);

        if (runtime.AuxiliaryValue > 0f)
        {
            runtime.AuxiliaryValue = MathF.Max(0f, runtime.AuxiliaryValue - dt);
            return;
        }

        if (travelDistance > maxDistance)
        {
            runtime.IsActive = false;
            return;
        }

        runtime.ScrollOffset -= dt;
        if (runtime.ScrollOffset <= 0f)
        {
            runtime.AuxiliaryValue = GetLightningFlashDuration(definition);
            runtime.ScrollOffset = RandomRange(GetLightningMinInterval(definition), GetLightningMaxInterval(definition));
            runtime.Phase = _random.Next(1, int.MaxValue);
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
            case SolarInstallStationPhase.Approaching:
            {
                if (_lastTravelDistance >= definition.TriggerDistance)
                {
                    runtime.Phase = (int)SolarInstallStationPhase.Docking;
                }

                break;
            }

            case SolarInstallStationPhase.Docking:
            {
                vehicle.ReleaseHandbrake();
                vehicle.SetPowered(false);
                vehicle.SetSolarDriveActive(false);

                float targetTravelDistance = GetSolarInstallStationDockedTravelDistance(definition);
                if (_lastTravelDistance >= targetTravelDistance)
                {
                    vehicle.SetBaseSpeed(0f);
                    runtime.Phase = (int)SolarInstallStationPhase.Ready;
                }
                else if (targetTravelDistance - _lastTravelDistance <= SolarInstallStationDockingSnapDistance)
                {
                    _lastTravelDistance = targetTravelDistance;
                    levelScene.SnapStoryDistance(targetTravelDistance);
                    vehicle.SetBaseSpeed(0f);
                    runtime.Phase = (int)SolarInstallStationPhase.Ready;
                }
                else
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
                    runtime.AuxiliaryValue = GetSolarInstallStationPanelWorldBounds(definition).Y;
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

    private void UpdateReturnCapsule(WorldEventDefinition definition, WorldEventRuntimeState runtime, float dt, Rectangle cameraViewBounds, LevelScene levelScene)
    {
        if (levelScene == null)
        {
            return;
        }

        ReturnCapsulePhase phase = (ReturnCapsulePhase)runtime.Phase;
        switch (phase)
        {
            case ReturnCapsulePhase.Ready:
            {
                Rectangle playerBounds = levelScene.PlayerBounds;
                if (playerBounds != Rectangle.Empty && playerBounds.Intersects(GetReturnCapsuleEntranceWorldBounds(definition, runtime)))
                {
                    levelScene.BeginReturnCapsuleLaunch();
                    runtime.Phase = (int)ReturnCapsulePhase.Launching;
                    runtime.AuxiliaryValue = 0f;
                }

                break;
            }

            case ReturnCapsulePhase.Launching:
            {
                runtime.AuxiliaryValue -= WorldConfig.ReturnCapsuleLaunchSpeed * dt;
                if (GetReturnCapsuleUpperWorldBounds(definition, runtime).Bottom < cameraViewBounds.Top - 32)
                {
                    runtime.Phase = (int)ReturnCapsulePhase.Completed;
                    runtime.IsActive = false;
                    levelScene.ChangeScene("end");
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

    private void DrawLightning(SpriteBatch spriteBatch, WorldEventDefinition definition, WorldEventRuntimeState runtime, Rectangle cameraViewBounds)
    {
        if (runtime.AuxiliaryValue <= 0f)
        {
            return;
        }

        float duration = GetLightningFlashDuration(definition);
        float flashProgress = duration <= 0f ? 1f : 1f - MathHelper.Clamp(runtime.AuxiliaryValue / duration, 0f, 1f);
        float baseAlpha = MathHelper.Clamp(definition.Alpha, 0f, 1f);
        float overlayPulse = GetStrongestLightningBoltPulse(definition, runtime, flashProgress);
        Color tint = Color.Lerp(Color.LightCyan, Color.White, 0.65f) * (baseAlpha * overlayPulse * 0.35f);
        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), cameraViewBounds, tint);
        DrawLightningBolts(spriteBatch, definition, runtime, cameraViewBounds, baseAlpha, flashProgress);
    }

    private static void DrawLightningBolts(SpriteBatch spriteBatch, WorldEventDefinition definition, WorldEventRuntimeState runtime, Rectangle cameraViewBounds, float baseAlpha, float flashProgress)
    {
        int boltCount = GetLightningBoltCount(definition);

        for (int bolt = 0; bolt < boltCount; bolt++)
        {
            Random boltRandom = new(GetLightningBoltSeed(runtime, bolt));
            float boltPulse = GetLightningBoltPulse(boltRandom, flashProgress);
            if (boltPulse <= 0f)
            {
                continue;
            }

            float alpha = baseAlpha * boltPulse;
            float startX = cameraViewBounds.Left + RandomRange(boltRandom, 32f, MathF.Max(32f, cameraViewBounds.Width - 32f));
            float endX = MathHelper.Clamp(
                startX + RandomRange(boltRandom, -260f, 260f),
                cameraViewBounds.Left + 16f,
                cameraViewBounds.Right - 16f);
            float endY = cameraViewBounds.Top + cameraViewBounds.Height * RandomRange(boltRandom, 0.45f, 0.95f);
            Vector2 previous = new(startX, cameraViewBounds.Top - 20f);

            for (int segment = 1; segment <= LightningSegmentCount; segment++)
            {
                float progress = segment / (float)LightningSegmentCount;
                float nextX = MathHelper.Lerp(startX, endX, progress) + RandomRange(boltRandom, -46f, 46f) * (1f - progress * 0.25f);
                float nextY = MathHelper.Lerp(cameraViewBounds.Top - 20f, endY, progress);
                Vector2 next = new(nextX, nextY);

                float thickness = MathHelper.Lerp(8f, 2f, progress);
                DrawLightningLine(spriteBatch, previous, next, thickness + 3f, Color.DeepSkyBlue * (alpha * 0.32f));
                DrawLightningLine(spriteBatch, previous, next, thickness, Color.White * alpha);
                DrawLightningLine(spriteBatch, previous, next, MathF.Max(1f, thickness * 0.45f), Color.LightCyan * alpha);

                if (segment > 2 && segment < LightningSegmentCount && boltRandom.NextDouble() < LightningBranchChance)
                {
                    DrawLightningBranch(spriteBatch, boltRandom, next, progress, alpha);
                }

                previous = next;
            }
        }
    }

    private static float GetStrongestLightningBoltPulse(WorldEventDefinition definition, WorldEventRuntimeState runtime, float flashProgress)
    {
        float strongestPulse = 0f;
        int boltCount = GetLightningBoltCount(definition);
        for (int bolt = 0; bolt < boltCount; bolt++)
        {
            Random boltRandom = new(GetLightningBoltSeed(runtime, bolt));
            strongestPulse = MathF.Max(strongestPulse, GetLightningBoltPulse(boltRandom, flashProgress));
        }

        return strongestPulse;
    }

    private static int GetLightningBoltSeed(WorldEventRuntimeState runtime, int boltIndex)
    {
        unchecked
        {
            int seed = runtime.Phase <= 0 ? 1 : runtime.Phase;
            return seed * 397 ^ (boltIndex + 1) * 7919;
        }
    }

    private static float GetLightningBoltPulse(Random boltRandom, float flashProgress)
    {
        float start = RandomRange(boltRandom, 0f, LightningBoltDelaySpread);
        float visibleWindow = RandomRange(boltRandom, LightningBoltMinVisibleWindow, LightningBoltMaxVisibleWindow);
        float localProgress = (flashProgress - start) / visibleWindow;
        if (localProgress < 0f || localProgress > 1f)
        {
            return 0f;
        }

        return MathF.Sin(localProgress * MathF.PI);
    }

    private static void DrawLightningBranch(SpriteBatch spriteBatch, Random boltRandom, Vector2 start, float mainProgress, float alpha)
    {
        int segmentCount = boltRandom.Next(2, 5);
        float direction = boltRandom.Next(0, 2) == 0 ? -1f : 1f;
        Vector2 previous = start;
        float branchLength = RandomRange(boltRandom, 42f, 120f) * (1f - mainProgress * 0.35f);
        float angle = RandomRange(boltRandom, 0.2f, 0.65f) * direction;

        for (int i = 1; i <= segmentCount; i++)
        {
            float progress = i / (float)segmentCount;
            Vector2 directionVector = new(
                MathF.Sin(angle) * branchLength * progress,
                MathF.Cos(angle) * branchLength * progress);
            Vector2 jitter = new(RandomRange(boltRandom, -18f, 18f), RandomRange(boltRandom, -8f, 18f));
            Vector2 next = start + directionVector + jitter;
            float fade = 1f - progress * 0.55f;

            DrawLightningLine(spriteBatch, previous, next, 4f * fade, Color.DeepSkyBlue * (alpha * 0.25f * fade));
            DrawLightningLine(spriteBatch, previous, next, 2f * fade, Color.LightCyan * (alpha * 0.85f * fade));
            previous = next;
        }
    }

    private static void DrawLightningLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, float thickness, Color color)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length <= 0.5f || thickness <= 0f)
        {
            return;
        }

        spriteBatch.Draw(
            AssetManager.GetTexture(Art.pixel),
            start,
            null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            Vector2.Zero,
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f);
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

        Rectangle animatedPanelBounds = runtime.Phase == (int)SolarInstallStationPhase.Installing
            ? GetSolarInstallAnimatedPanelWorldBounds(runtime.AuxiliaryValue)
            : GetSolarInstallStationPanelWorldBounds(definition);
        Art art = Game1.SolarPanelHasEnergy ? Art.SolarPanelPowered : Art.SolarPanelUnpowered;
        spriteBatch.Draw(AssetManager.GetTexture(art), animatedPanelBounds, Color.White);
    }

    private void DrawReturnCapsuleWorld(SpriteBatch spriteBatch, WorldEventRuntimeState runtime, WorldEventDefinition definition, Rectangle cameraViewBounds)
    {
        Rectangle lowerBounds = GetReturnCapsuleLowerWorldBounds(definition);
        Rectangle upperBounds = GetReturnCapsuleUpperWorldBounds(definition, runtime);
        if (lowerBounds.Right < cameraViewBounds.Left - 64 || lowerBounds.Left > cameraViewBounds.Right + 64)
        {
            return;
        }

        spriteBatch.Draw(AssetManager.GetTexture(Art.ApolloUpperHalf), upperBounds, Color.White);
        spriteBatch.Draw(AssetManager.GetTexture(Art.ApolloLowerHalf), lowerBounds, Color.White);
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
        if (string.Equals(definition.Type, SolarInstallStationType, StringComparison.OrdinalIgnoreCase))
        {
            return MathF.Max(0f, definition.TriggerDistance - SolarInstallStationLeadInDistance);
        }

        return IsDistanceRangeWeather(definition)
            ? GetWeatherMinDistance(definition)
            : definition.TriggerDistance;
    }

    private static float GetSandstormMinDistance(WorldEventDefinition definition)
    {
        return GetWeatherMinDistance(definition);
    }

    private static float GetSandstormMaxDistance(WorldEventDefinition definition)
    {
        return GetWeatherMaxDistance(definition);
    }

    private static bool IsDistanceRangeWeather(WorldEventDefinition definition)
    {
        return string.Equals(definition.Type, SandstormType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(definition.Type, LightningType, StringComparison.OrdinalIgnoreCase);
    }

    private static float GetWeatherMinDistance(WorldEventDefinition definition)
    {
        return MathF.Max(0f, definition.MinDistance);
    }

    private static float GetWeatherMaxDistance(WorldEventDefinition definition)
    {
        return MathF.Max(GetWeatherMinDistance(definition), definition.MaxDistance);
    }

    private static float GetLightningMinInterval(WorldEventDefinition definition)
    {
        return definition.MinInterval > 0f ? definition.MinInterval : LightningDefaultMinInterval;
    }

    private static float GetLightningMaxInterval(WorldEventDefinition definition)
    {
        return MathF.Max(GetLightningMinInterval(definition), definition.MaxInterval > 0f ? definition.MaxInterval : LightningDefaultMaxInterval);
    }

    private static float GetLightningFlashDuration(WorldEventDefinition definition)
    {
        return definition.FlashDuration > 0f ? definition.FlashDuration : LightningDefaultFlashDuration;
    }

    private static int GetLightningBoltCount(WorldEventDefinition definition)
    {
        return Math.Max(1, definition.BoltCount > 0 ? definition.BoltCount : LightningDefaultBoltCount);
    }

    private float RandomRange(float min, float max)
    {
        if (max <= min)
        {
            return min;
        }

        return min + (float)_random.NextDouble() * (max - min);
    }

    private static float RandomRange(Random random, float min, float max)
    {
        if (max <= min)
        {
            return min;
        }

        return min + (float)random.NextDouble() * (max - min);
    }

    private static bool ShouldShowSolarInstallButton(WorldEventRuntimeState runtime)
    {
        return runtime.Phase == (int)SolarInstallStationPhase.Approaching
            || runtime.Phase == (int)SolarInstallStationPhase.Docking
            || runtime.Phase == (int)SolarInstallStationPhase.Ready
            || runtime.Phase == (int)SolarInstallStationPhase.Installing
            || runtime.Phase == (int)SolarInstallStationPhase.Completed;
    }

    private static bool CanPressSolarInstallButton(WorldEventRuntimeState runtime)
    {
        return !Game1.SolarPanelEnabled && runtime.Phase == (int)SolarInstallStationPhase.Ready;
    }

    private bool AreSolarInstallStationCollisionsActive(WorldEventDefinition definition, WorldEventRuntimeState runtime)
    {
        SolarInstallStationPhase phase = (SolarInstallStationPhase)runtime.Phase;
        if (phase == SolarInstallStationPhase.Ready
            || phase == SolarInstallStationPhase.Installing
            || phase == SolarInstallStationPhase.Completed)
        {
            return true;
        }

        return phase == SolarInstallStationPhase.Docking
            && _lastTravelDistance >= GetSolarInstallStationDockedTravelDistance(definition);
    }

    private string GetSolarInstallStationDebugStatus(WorldEventRuntimeState runtime)
    {
        return ((SolarInstallStationPhase)runtime.Phase) switch
        {
            SolarInstallStationPhase.Approaching => "solar_install_station approaching",
            SolarInstallStationPhase.Docking => "solar_install_station docking",
            SolarInstallStationPhase.Ready => "solar_install_station ready",
            SolarInstallStationPhase.Installing => "solar_install_station installing",
            SolarInstallStationPhase.Completed => "solar_install_station completed",
            _ => "solar_install_station"
        };
    }

    private static string GetReturnCapsuleDebugStatus(WorldEventRuntimeState runtime)
    {
        return ((ReturnCapsulePhase)runtime.Phase) switch
        {
            ReturnCapsulePhase.Ready => "return_capsule ready",
            ReturnCapsulePhase.Launching => "return_capsule launching",
            ReturnCapsulePhase.Completed => "return_capsule completed",
            _ => "return_capsule"
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

    private Rectangle GetSolarInstallStationPanelWorldBounds(WorldEventDefinition definition)
    {
        Rectangle stationBounds = GetSolarInstallStationWorldBounds(definition);
        return new Rectangle(
            stationBounds.X + (int)MathF.Round(WorldConfig.SolarInstallAnimatedPanelTopLeftLocal.X),
            stationBounds.Y + (int)MathF.Round(WorldConfig.SolarInstallAnimatedPanelTopLeftLocal.Y),
            WorldConfig.SolarPanelSize.X,
            WorldConfig.SolarPanelSize.Y);
    }

    private static float GetSolarInstallStationBaseWorldLocalX(WorldEventDefinition definition)
    {
        return definition.TriggerDistance + WorldConfig.SolarInstallStationStartScreenX;
    }

    private static float GetSolarInstallStationDockedTravelDistance(WorldEventDefinition definition)
    {
        return GetSolarInstallStationBaseWorldLocalX(definition) - WorldConfig.SolarInstallStationDockedScreenX;
    }

    private Rectangle GetReturnCapsuleLowerWorldBounds(WorldEventDefinition definition)
    {
        int x = (int)MathF.Round(GetReturnCapsuleBaseWorldLocalX(definition) - _lastTravelDistance);
        int y = WorldConfig.FakeGroundLocalRect.Y - WorldConfig.ReturnCapsuleSize.Y;
        return new Rectangle(x, y, WorldConfig.ReturnCapsuleSize.X, WorldConfig.ReturnCapsuleSize.Y);
    }

    private Rectangle GetReturnCapsuleUpperWorldBounds(WorldEventDefinition definition, WorldEventRuntimeState runtime)
    {
        Rectangle lowerBounds = GetReturnCapsuleLowerWorldBounds(definition);
        return new Rectangle(
            lowerBounds.X,
            (int)MathF.Round(lowerBounds.Y + runtime.AuxiliaryValue),
            lowerBounds.Width,
            lowerBounds.Height);
    }

    private Rectangle GetReturnCapsuleLowerCollisionWorldBounds(WorldEventDefinition definition)
    {
        Rectangle lowerBounds = GetReturnCapsuleLowerWorldBounds(definition);
        Rectangle local = WorldConfig.ReturnCapsuleLowerCollisionLocalRect;
        return new Rectangle(lowerBounds.X + local.X, lowerBounds.Y + local.Y, local.Width, local.Height);
    }

    private Rectangle GetReturnCapsuleEntranceWorldBounds(WorldEventDefinition definition, WorldEventRuntimeState runtime)
    {
        Rectangle upperBounds = GetReturnCapsuleUpperWorldBounds(definition, runtime);
        Rectangle local = WorldConfig.ReturnCapsuleEntranceLocalRect;
        return new Rectangle(upperBounds.X + local.X, upperBounds.Y + local.Y, local.Width, local.Height);
    }

    private static float GetReturnCapsuleBaseWorldLocalX(WorldEventDefinition definition)
    {
        return definition.TriggerDistance + WorldConfig.ReturnCapsuleSpawnScreenX;
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
