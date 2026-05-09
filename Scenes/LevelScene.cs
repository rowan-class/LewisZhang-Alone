using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class LevelScene : Scene, IPlayerScene
{
    private const float DebugSpeedAdjustPerSecond = 240f;
    private const float RepairFlashDuration = 0.25f;
    private const int DamageFailureThreshold = 2;
    private const float DamageFailureDuration = 10f;
    private const float DamageFailureDeathMaxDistance = 35000f;
    private const float OffscreenFailureDuration = 10f;
    private const float SolarInstallStationTriggerDistance = 4300f;
    private const float RockEventClearance = 320f;
    private const float InitialRockClearDistance = 500f;
    private const float FuelBarrelSpawnChance = 0.62f;
    private const int FuelBarrelSpawnMinSpacing = 520;
    private const int FuelBarrelSpawnMaxSpacing = 920;
    private const float TutorialPromptScale = 1.28f;

    private static readonly RockObstacle[] WorldRockObstacles = CreateWorldRockObstacles();

    private readonly Camera2D _camera = new();
    private readonly WorldEventManager _eventManager = new();
    private readonly TextBoxEntity _textBox = new();
    private readonly VehicleEntity _vehicle = new();
    private readonly ThrottleEntity _throttle;
    private readonly FuelDisplayEntity _fuelDisplay;
    private readonly FuelPortEntity _fuelPort;
    private readonly FuelButtonModuleEntity _fuelButton;
    private readonly HandbrakeButtonEntity _handbrakeButton;
    private readonly SolarButtonEntity _solarButton;
    private readonly AutoPickupModuleEntity _autoPickupModule;
    private readonly SolarPanelEntity _solarPanel;
    private readonly Player _player;
    private readonly GroundEntity _ground = new();
    private readonly List<FuelBarrelEntity> _fuelBarrels = new();
    private readonly List<RepairGunEntity> _repairGuns = new();
    private readonly Random _random = new(7);

    private float _worldScrollX;
    private float _nextFuelSpawnX = 2400f;
    private float _repairFlashTimer;
    private float _damageFailureTimer;
    private float _offscreenFailureTimer;
    private float _moduleDamageBlinkTimer;
    private bool _preferOverviewView;
    private bool _vehicleDiscovered = true;
    private bool _initialFuelBarrelsSpawned;
    private bool _repairGunExplained;
    private bool _isReturnCapsuleLaunching;
    private bool _isPaused;
    private bool _enteredFromIntroWalkCarryingFuelBarrel;
    private bool _throttleTutorialCompleted;
    private bool _fuelTutorialCompleted;
    private FuelBarrelEntity _carriedFuelBarrel;
    private RepairGunEntity _carriedRepairGun;

    public LevelScene(float debugStoryDistance)
        : this()
    {
        ApplyDebugStoryDistance(debugStoryDistance);
    }

    public LevelScene(SaveData saveData = null)
    {
        if (saveData == null)
        {
            Game1.SolarPanelEnabled = false;
            Game1.ResetSolarPanelEnergyState();
        }

        _throttle = new ThrottleEntity(_vehicle);
        _fuelDisplay = new FuelDisplayEntity(_vehicle);
        _fuelPort = new FuelPortEntity();
        _fuelButton = new FuelButtonModuleEntity(_vehicle, _fuelPort);
        _handbrakeButton = new HandbrakeButtonEntity(_vehicle, _throttle);
        _autoPickupModule = new AutoPickupModuleEntity();
        _solarPanel = new SolarPanelEntity();
        _solarButton = new SolarButtonEntity(_vehicle, _solarPanel);
        _player = new Player(WorldConfig.PlayerStartPosition);
        _vehicle.SetScene(this);
        _throttle.SetScene(this);
        _fuelDisplay.SetScene(this);
        _fuelPort.SetScene(this);
        _fuelButton.SetScene(this);
        _handbrakeButton.SetScene(this);
        _solarButton.SetScene(this);
        _autoPickupModule.SetScene(this);
        _solarPanel.SetScene(this);
        _player.SetScene(this);
        _ground.SetScene(this);
        _ground.Update(new GameTime());

        if (saveData != null)
        {
            RestoreSaveData(saveData);
            _throttleTutorialCompleted = true;
            _fuelTutorialCompleted = true;
        }
        else
        {
            bool enteredFromIntroWalk = ApplyIntroTransition();
            SpawnInitialFuelBarrels(includeVehicleLeftBarrel: !enteredFromIntroWalk);
        }

        UpdateCameraMode();
    }

    public float WorldScrollX => _worldScrollX;
    public float StoryDistance => _worldScrollX;
    public float VehicleSpeed => _vehicle.Speed;
    public VehicleEntity Vehicle => _vehicle;
    public bool IsPlayerCarryingFuelBarrel => _carriedFuelBarrel != null;
    public bool IsPlayerCarryingItem => _carriedFuelBarrel != null || _carriedRepairGun != null;
    public Player Player => _player;
    public Rectangle PlayerBounds => _player.GetBounds();
    public bool IsThrottleLockedByEvent => _eventManager.IsThrottleLocked(StoryDistance);
    public bool IsPlayerInputLocked => _eventManager.IsPlayerMovementLocked();
    public bool CanPlayerMoveLeft => true;
    public Rectangle PlayerMovementBounds => new(0, 0, WorldConfig.WorldWidth, WorldConfig.WorldHeight);
    public Rectangle GroundCollisionBounds
    {
        get
        {
            Rectangle viewBounds = _camera.ViewBounds;
            int leftPadding = WorldConfig.WorldWidth;
            int rightPadding = WorldConfig.WorldWidth + 2000;
            return new Rectangle(
                viewBounds.Left - leftPadding,
                WorldConfig.FakeGroundLocalRect.Y,
                viewBounds.Width + leftPadding + rightPadding,
                WorldConfig.FakeGroundLocalRect.Height);
        }
    }

    public IEnumerable<FuelBarrelEntity> GetFuelBarrels()
    {
        return _fuelBarrels;
    }

    public IEnumerable<Rectangle> GetWheelSuspensionObstacleBounds()
    {
        foreach (RockObstacle obstacle in WorldRockObstacles)
        {
            Rectangle bounds = GetWorldRockCollisionBounds(obstacle);
            if (bounds.Right < _camera.ViewBounds.Left - 160 || bounds.Left > _camera.ViewBounds.Right + 160)
            {
                continue;
            }

            yield return bounds;
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (ServiceLocator.Input.IsActionPressed(Action.TogglePause))
        {
            _isPaused = !_isPaused;
        }

        if (_isPaused)
        {
            return;
        }

        if (_textBox.IsActiveDialogue)
        {
            _textBox.Update(gameTime);
            return;
        }

        if (_isReturnCapsuleLaunching)
        {
            UpdateReturnCapsuleEvent(gameTime);
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _moduleDamageBlinkTimer += dt;

        if (ServiceLocator.Input.IsActionPressed(Action.ToggleCameraView))
        {
            _preferOverviewView = !_preferOverviewView;
        }

        if (ServiceLocator.Input.IsActionPressed(Action.SaveGame))
        {
            SaveManager.Save(CaptureSaveData());
        }

        if (Game1.Debug && ServiceLocator.Input.IsActionPressed(Action.ToggleAutoPickupModule))
        {
            _autoPickupModule.ToggleEnabled();
        }

        if (Game1.Debug && ServiceLocator.Input.IsActionPressed(Action.ToggleSolarPanelModule))
        {
            Game1.SolarPanelEnabled = !Game1.SolarPanelEnabled;
            if (!Game1.SolarPanelEnabled || !Game1.SolarPanelHasEnergy)
            {
                _vehicle.SetSolarDriveActive(false);
            }
        }

        if (Game1.Debug && ServiceLocator.Input.IsActionPressed(Action.DebugRepairAll))
        {
            DebugRepairAllComponents();
        }

        _ground.Update(gameTime);
        _eventManager.Update(gameTime, StoryDistance, _camera.ViewBounds, this);
        if (_isReturnCapsuleLaunching)
        {
            _camera.Update(gameTime, 0f);
            return;
        }

        if (_textBox.IsActiveDialogue)
        {
            UpdateCameraMode();
            _camera.Update(gameTime, _vehicle.Speed);
            return;
        }

        SpawnFuelBarrelsIfNeeded();

        _player.Update(gameTime);

        _solarButton.Update(gameTime);
        _handbrakeButton.Update(gameTime);
        _throttle.Update(gameTime);
        _vehicle.Update(gameTime);
        _fuelPort.Update(gameTime);
        float fuelBeforeButtonUpdate = _vehicle.Fuel;
        _fuelButton.Update(gameTime);
        if (_vehicle.Fuel > fuelBeforeButtonUpdate)
        {
            _fuelTutorialCompleted = true;
        }

        if (_throttle.State == ThrottleState.ActiveHold || _vehicle.Powered)
        {
            _throttleTutorialCompleted = true;
        }

        _solarPanel.Update(gameTime);

        if (Game1.Debug)
        {
            HandleDebugSpeedControls(dt);
        }

        _worldScrollX += _vehicle.Speed * dt;

        _fuelDisplay.Update(gameTime);

        UpdateFuelBarrels(gameTime);
        UpdateRepairGuns(gameTime);
        TryRepairDamagedComponent();
        HandleFuelBarrelInteraction();
        _autoPickupModule.Update(gameTime);
        UpdateDamageFailureState(dt);

        _repairFlashTimer = MathF.Max(0f, _repairFlashTimer - dt);
        UpdateCameraMode();
        _camera.Update(gameTime, _vehicle.Speed);
        UpdateOffscreenFailureState(dt);
        CleanupFuelBarrels();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        bool playerOutsideVehicle = !IsPlayerInsideVehicle(_player);

        spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix(), samplerState: SamplerState.PointClamp);
        DrawBackground(spriteBatch);
        _eventManager.DrawWorld(spriteBatch, _camera.ViewBounds);
        _ground.Draw(spriteBatch);
        _vehicle.DrawBody(spriteBatch);
        _throttle.Draw(spriteBatch);
        _fuelDisplay.Draw(spriteBatch);
        _fuelPort.Draw(spriteBatch);
        _fuelButton.Draw(spriteBatch);
        _handbrakeButton.Draw(spriteBatch);
        _solarButton.Draw(spriteBatch);
        _solarPanel.Draw(spriteBatch);
        DrawFuelBarrels(spriteBatch, carriedOnly: false, aboveWheels: false);
        DrawRepairGuns(spriteBatch, carriedOnly: false, aboveWheels: false);
        DrawFuelBarrels(spriteBatch, carriedOnly: true, aboveWheels: false);
        DrawRepairGuns(spriteBatch, carriedOnly: true, aboveWheels: false);
        if (!playerOutsideVehicle && !_isReturnCapsuleLaunching)
        {
            _player.Draw(spriteBatch);
        }
        _vehicle.DrawWheelsLayer(spriteBatch);
        DrawWorldRockObstacles(spriteBatch);
        DrawFuelBarrels(spriteBatch, carriedOnly: false, aboveWheels: true);
        DrawRepairGuns(spriteBatch, carriedOnly: false, aboveWheels: true);
        DrawFuelBarrels(spriteBatch, carriedOnly: true, aboveWheels: true);
        DrawRepairGuns(spriteBatch, carriedOnly: true, aboveWheels: true);
        if (playerOutsideVehicle && !_isReturnCapsuleLaunching)
        {
            _player.Draw(spriteBatch);
        }

        _autoPickupModule.Draw(spriteBatch);
        _eventManager.DrawOverlay(spriteBatch, _camera.ViewBounds);

        if (Game1.Debug)
        {
            DrawDebugRectangles(spriteBatch, GetDebugRectangles());
            DrawDebugRectangle(spriteBatch, _vehicle.CabinBoundsWorld, Color.LimeGreen);
        }

        spriteBatch.End();

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawHud(spriteBatch);
        DrawRepairFlash(spriteBatch);
        DrawDamageFailureWarning(spriteBatch);
        DrawOffscreenFailureWarning(spriteBatch);
        if (!_isPaused)
        {
            DrawPauseHint(spriteBatch);
        }
        _textBox.Draw(spriteBatch);
        DrawPauseOverlay(spriteBatch);
        spriteBatch.End();
    }

    public Vector2 ResolvePosition(PositionSpace space, Vector2 localPosition)
    {
        return space switch
        {
            PositionSpace.World => new Vector2(localPosition.X - _worldScrollX, localPosition.Y),
            PositionSpace.Vehicle => _vehicle.Position + localPosition,
            _ => localPosition
        };
    }

    public Vector2 WorldToLocal(PositionSpace space, Vector2 worldPosition)
    {
        return space switch
        {
            PositionSpace.World => new Vector2(worldPosition.X + _worldScrollX, worldPosition.Y),
            PositionSpace.Vehicle => worldPosition - _vehicle.Position,
            _ => worldPosition
        };
    }

    public void SnapStoryDistance(float storyDistance)
    {
        _worldScrollX = MathF.Max(0f, storyDistance);
    }

    public IEnumerable<Rectangle> GetSolidRectangles()
    {
        yield return _ground.GetBounds();

        foreach (Rectangle vehicleRect in _vehicle.GetInteriorCollisionWorldRectangles())
        {
            yield return vehicleRect;
        }

        foreach (Rectangle rect in _eventManager.GetSolidRectangles())
        {
            yield return rect;
        }

        foreach (RockObstacle obstacle in WorldRockObstacles)
        {
            yield return GetWorldRockCollisionBounds(obstacle);
        }

        Rectangle throttleRect = _throttle.GetCollisionBounds();
        if (throttleRect != Rectangle.Empty)
        {
            yield return throttleRect;
        }
    }

    public bool IsPlayerInsideVehicle(Player player)
    {
        return _vehicle.IsInsideCabin(player.GetBounds());
    }

    public bool IsPlayerAttachedToVehicle(Player player)
    {
        return IsPlayerInsideVehicle(player);
    }

    public void DamageComponent(string component)
    {
        switch (component?.Trim().ToLowerInvariant())
        {
            case "fuel_port":
            case "fuel":
                _fuelPort.Damage();
                break;

            case "solar":
            case "solar_panel":
                _solarPanel.Damage();
                _vehicle.SetSolarDriveActive(false);
                break;

            case "throttle":
                _throttle.Damage();
                break;

            case "auto_pickup":
            case "auto_pickup_module":
                _autoPickupModule.Damage();
                break;

            case "repair_gun":
            case "repairgun":
                BreakRepairGuns();
                break;
        }
    }

    public void ReachVehicle()
    {
        if (_vehicleDiscovered)
        {
            return;
        }

        _vehicleDiscovered = true;
        _preferOverviewView = true;
        SpawnInitialFuelBarrels();
        _throttle.Update(new GameTime());
        _fuelDisplay.Update(new GameTime());
        _fuelPort.Update(new GameTime());
        _fuelButton.Update(new GameTime());
        _handbrakeButton.Update(new GameTime());
        _solarButton.Update(new GameTime());
        _autoPickupModule.Update(new GameTime());
        _solarPanel.Update(new GameTime());
    }

    public void SpawnRepairGunDrop(float worldLocalX)
    {
        foreach (RepairGunEntity existingRepairGun in _repairGuns)
        {
            if (existingRepairGun.IsActive)
            {
                return;
            }
        }

        float groundY = WorldConfig.FakeGroundLocalRect.Y - WorldConfig.RepairGunSize.Y;
        RepairGunEntity repairGun = new(PositionSpace.World, new Vector2(worldLocalX, groundY));
        repairGun.SetScene(this);
        repairGun.Update(new GameTime());
        _repairGuns.Add(repairGun);
    }

    public void StartDialogue(IEnumerable<DialogueLine> lines)
    {
        _textBox.StartDialogue(lines);
    }

    public void BeginReturnCapsuleLaunch()
    {
        if (_isReturnCapsuleLaunching)
        {
            return;
        }

        _isReturnCapsuleLaunching = true;
        _vehicle.SetSolarDriveActive(false);
        _vehicle.SetPowered(false);
        _vehicle.SetBaseSpeed(0f);
        _player.Deactivate();

        if (_carriedFuelBarrel != null)
        {
            _carriedFuelBarrel.Deactivate();
            _carriedFuelBarrel = null;
        }

        if (_carriedRepairGun != null)
        {
            _carriedRepairGun.Deactivate();
            _carriedRepairGun = null;
        }
    }

    public void BeginFinalWalkTransition()
    {
        SceneTransitionContext.LevelToFinalWalk = new LevelToFinalWalkTransition
        {
            PlayerVehicleLocalPosition = _player.Position - _vehicle.Position,
            ModuleDamageBlinkTimer = _moduleDamageBlinkTimer,
            FuelRatio = _vehicle.FuelRatio,
            FuelPortLit = _fuelPort.IsLit,
            FuelPortDamaged = _fuelPort.IsDamaged,
            SolarPanelInstalled = Game1.SolarPanelEnabled,
            SolarPanelDamaged = _solarPanel.IsDamaged,
            ThrottleDamaged = _throttle.IsDamaged,
            AutoPickupDamaged = _autoPickupModule.IsDamaged,
            HandbrakeActive = _vehicle.HandbrakeActive,
            SolarDriveActive = _vehicle.SolarDriveActive
        };

        ChangeScene("finalWalk");
    }

    protected override IEnumerable<Rectangle> GetDebugRectangles()
    {
        foreach (Rectangle rect in _ground.GetDebugRectangles())
        {
            yield return rect;
        }

        foreach (Rectangle rect in _vehicle.GetDebugRectangles())
        {
            yield return rect;
        }

        foreach (Rectangle rect in _throttle.GetDebugRectangles())
        {
            yield return rect;
        }

        foreach (Rectangle rect in _fuelDisplay.GetDebugRectangles())
        {
            yield return rect;
        }

        foreach (Rectangle rect in _fuelPort.GetDebugRectangles())
        {
            yield return rect;
        }

        foreach (Rectangle rect in _fuelButton.GetDebugRectangles())
        {
            yield return rect;
        }

        foreach (Rectangle rect in _handbrakeButton.GetDebugRectangles())
        {
            yield return rect;
        }

        foreach (Rectangle rect in _solarButton.GetDebugRectangles())
        {
            yield return rect;
        }

        foreach (Rectangle rect in _autoPickupModule.GetDebugRectangles())
        {
            yield return rect;
        }

        foreach (Rectangle rect in _solarPanel.GetDebugRectangles())
        {
            yield return rect;
        }

        foreach (Rectangle rect in _eventManager.GetDebugRectangles())
        {
            yield return rect;
        }

        foreach (RockObstacle obstacle in WorldRockObstacles)
        {
            yield return GetWorldRockCollisionBounds(obstacle);
        }

        foreach (Rectangle rect in _player.GetDebugRectangles())
        {
            yield return rect;
        }

        foreach (FuelBarrelEntity barrel in _fuelBarrels)
        {
            foreach (Rectangle rect in barrel.GetDebugRectangles())
            {
                yield return rect;
            }
        }

        foreach (RepairGunEntity repairGun in _repairGuns)
        {
            foreach (Rectangle rect in repairGun.GetDebugRectangles())
            {
                yield return rect;
            }
        }
    }

    private void UpdateFuelBarrels(GameTime gameTime)
    {
        foreach (FuelBarrelEntity barrel in _fuelBarrels)
        {
            if (barrel.IsActive)
            {
                barrel.Update(gameTime);
            }
        }
    }

    private bool ApplyIntroTransition()
    {
        IntroToLevelTransition transition = SceneTransitionContext.ConsumeIntroToLevel();
        if (transition == null)
        {
            return false;
        }

        _player.SetPosition(_vehicle.Position + transition.PlayerVehicleLocalPosition);
        _preferOverviewView = false;
        _enteredFromIntroWalkCarryingFuelBarrel = transition.IsCarryingFuelBarrel;
        if (transition.IsCarryingFuelBarrel)
        {
            FuelBarrelEntity barrel = new(PositionSpace.Vehicle, transition.PlayerVehicleLocalPosition);
            barrel.SetScene(this);
            barrel.PickUp(_player);
            barrel.Update(new GameTime());
            _fuelBarrels.Add(barrel);
            _carriedFuelBarrel = barrel;
        }

        return true;
    }

    private void ApplyDebugStoryDistance(float debugStoryDistance)
    {
        if (debugStoryDistance <= 0f)
        {
            return;
        }

        SnapStoryDistance(debugStoryDistance);
        _eventManager.ApplyDebugStoryDistance(debugStoryDistance, this);
        _nextFuelSpawnX = MathF.Max(_nextFuelSpawnX, debugStoryDistance + WorldConfig.WorldWidth);
        _preferOverviewView = true;
        _throttleTutorialCompleted = true;
        _fuelTutorialCompleted = true;
        UpdateCameraMode();
        _camera.Update(new GameTime(), 0f);
    }

    private void DrawFuelBarrels(SpriteBatch spriteBatch, bool carriedOnly, bool aboveWheels)
    {
        foreach (FuelBarrelEntity barrel in _fuelBarrels)
        {
            if (barrel.IsActive
                && barrel.IsCarried == carriedOnly
                && ShouldDrawFuelBarrelAboveWheels(barrel) == aboveWheels)
            {
                barrel.Draw(spriteBatch);
            }
        }
    }

    private bool ShouldDrawFuelBarrelAboveWheels(FuelBarrelEntity barrel)
    {
        return barrel.IsCarried
            ? !IsPlayerInsideVehicle(_player)
            : barrel.Space == PositionSpace.World;
    }

    private void UpdateRepairGuns(GameTime gameTime)
    {
        foreach (RepairGunEntity repairGun in _repairGuns)
        {
            if (repairGun.IsActive)
            {
                repairGun.Update(gameTime);
            }
        }
    }

    private void DrawRepairGuns(SpriteBatch spriteBatch, bool carriedOnly, bool aboveWheels)
    {
        foreach (RepairGunEntity repairGun in _repairGuns)
        {
            if (repairGun.IsActive
                && repairGun.IsCarried == carriedOnly
                && ShouldDrawRepairGunAboveWheels(repairGun) == aboveWheels)
            {
                repairGun.Draw(spriteBatch);
            }
        }
    }

    private bool ShouldDrawRepairGunAboveWheels(RepairGunEntity repairGun)
    {
        return repairGun.IsCarried
            ? !IsPlayerInsideVehicle(_player)
            : repairGun.Space == PositionSpace.World;
    }

    private void DrawWorldRockObstacles(SpriteBatch spriteBatch)
    {
        foreach (RockObstacle obstacle in WorldRockObstacles)
        {
            Rectangle bounds = GetWorldRockBounds(obstacle);
            if (bounds.Right < _camera.ViewBounds.Left - 128 || bounds.Left > _camera.ViewBounds.Right + 128)
            {
                continue;
            }

            DrawRockObstacle(spriteBatch, obstacle.Art, bounds);
        }
    }

    private static void DrawRockObstacle(SpriteBatch spriteBatch, Art art, Rectangle bounds)
    {
        spriteBatch.Draw(AssetManager.GetTexture(art), bounds, Color.White);

        float nightBlend = MathHelper.Clamp(Game1.BackgroundNightBlend, 0f, 1f);
        if (nightBlend <= 0f)
        {
            return;
        }

        spriteBatch.Draw(AssetManager.GetTexture(GetNightRockArt(art)), bounds, Color.White * nightBlend);
    }

    private static Art GetNightRockArt(Art art)
    {
        return art switch
        {
            Art.Rock1 => Art.Rock1Night,
            Art.Rock2 => Art.Rock2Night,
            Art.Rock3 => Art.Rock3Night,
            _ => art
        };
    }

    private Rectangle GetWorldRockBounds(RockObstacle obstacle)
    {
        Vector2 worldPosition = ResolvePosition(PositionSpace.World, new Vector2(
            obstacle.WorldLocalX,
            WorldConfig.FakeGroundLocalRect.Y + 50f - obstacle.Size.Y));

        return new Rectangle(
            (int)MathF.Round(worldPosition.X),
            (int)MathF.Round(worldPosition.Y),
            obstacle.Size.X,
            obstacle.Size.Y);
    }

    private Rectangle GetWorldRockCollisionBounds(RockObstacle obstacle)
    {
        Rectangle bounds = GetWorldRockBounds(obstacle);
        return new Rectangle(
            bounds.X + 16,
            bounds.Y + 8,
            Math.Max(1, bounds.Width - 32),
            Math.Max(1, bounds.Height - 8));
    }

    private static RockObstacle[] CreateWorldRockObstacles()
    {
        Random random = new(31);
        List<RockObstacle> obstacles = new();
        float x = 1250f;

        while (x < 46800f)
        {
            x += random.Next(520, 980);
            RockObstacle obstacle = CreateRandomRockObstacle(random, x);

            if (IsWorldRockBlockedByLargeEvent(obstacle.WorldLocalX, obstacle.Size.X))
            {
                continue;
            }

            obstacles.Add(obstacle);
        }

        return obstacles.ToArray();
    }

    private static RockObstacle CreateRandomRockObstacle(Random random, float worldLocalX)
    {
        return random.Next(0, 3) switch
        {
            0 => new RockObstacle(Art.Rock1, worldLocalX, new Point(127, 73)),
            1 => new RockObstacle(Art.Rock2, worldLocalX, new Point(250, 100)),
            _ => new RockObstacle(Art.Rock3, worldLocalX, new Point(250, 100))
        };
    }

    private static bool IsWorldRockBlockedByLargeEvent(float worldLocalX, int width)
    {
        float right = worldLocalX + width;
        float initialClearRight = WorldConfig.VehiclePosition.X + WorldConfig.VehicleSize.X + InitialRockClearDistance;
        float solarStationLeft = SolarInstallStationTriggerDistance + WorldConfig.SolarInstallStationStartScreenX;
        float solarStationRight = solarStationLeft + WorldConfig.SolarInstallStationSize.X;

        return RangesOverlap(worldLocalX, right, 0f, initialClearRight)
            || RangesOverlap(worldLocalX, right, 3300f, 6800f)
            || RangesOverlap(worldLocalX, right, solarStationLeft - RockEventClearance, solarStationRight + RockEventClearance)
            || RangesOverlap(worldLocalX, right, 11150f, 12900f)
            || RangesOverlap(worldLocalX, right, 14850f, 16350f)
            || RangesOverlap(worldLocalX, right, 18500f, 21250f)
            || RangesOverlap(worldLocalX, right, 24000f, 27350f)
            || RangesOverlap(worldLocalX, right, 32050f, 33600f)
            || RangesOverlap(worldLocalX, right, 39950f, 43150f)
            || RangesOverlap(worldLocalX, right, 44750f, 45850f)
            || RangesOverlap(worldLocalX, right, 46500f, 49000f);
    }

    private static bool RangesOverlap(float leftA, float rightA, float leftB, float rightB)
    {
        return leftA < rightB && rightA > leftB;
    }

    private void DrawBackground(SpriteBatch spriteBatch)
    {
        BackgroundRenderer.DrawLooping(spriteBatch, _worldScrollX, WorldConfig.WorldWidth);
    }

    private void DrawHud(SpriteBatch spriteBatch)
    {
        DrawLevelTutorialPrompts(spriteBatch);

        string powerState = _vehicle.Powered ? "Powered" : "No Power";
        string cameraState = ShouldUseOverviewCamera() ? "Overview" : "Interior";
        string carryingState = _carriedFuelBarrel != null ? "Fuel Barrel" : _carriedRepairGun != null ? "Repair Gun" : "None";

        if (!Game1.Debug)
        {
            return;
        }

        string solarCondition = Game1.SolarPanelBlockedByWeather ? "Sandstorm" : Game1.SolarPanelIsDaytime ? "Day" : "Night";
        DrawSimpleDebugText(spriteBatch, "DEBUG", new List<HudLine>
        {
            new HudLine("Vehicle  " + powerState + "    Speed  " + _vehicle.Speed.ToString("0.0"), new Color(189, 235, 255)),
            new HudLine("Camera  " + cameraState + "    Carrying  " + carryingState, new Color(189, 235, 255)),
            new HudLine("Player  " + _player.CurrentState + "    Distance  " + StoryDistance.ToString("0.0"), Color.LightGreen),
            new HudLine("Auto pickup  " + (_autoPickupModule.IsEnabled ? "On" : "Off") + "    " + (_autoPickupModule.IsDamaged ? "Broken" : "OK"), Color.Yellow),
            new HudLine("Solar  " + (Game1.SolarPanelEnabled ? "On" : "Off") + "    " + solarCondition, Color.Cyan),
            new HudLine("Event  " + _eventManager.GetDebugStatus(), Color.Orange),
            new HudLine("Damage  Fuel " + (_fuelPort.IsDamaged ? "Broken" : "OK") + "    Solar " + (_solarPanel.IsDamaged ? "Broken" : "OK") + "    Throttle " + (_throttle.IsDamaged ? "Broken" : "OK") + "    Pickup " + (_autoPickupModule.IsDamaged ? "Broken" : "OK"), Color.OrangeRed),
            new HudLine("Debug keys  [ / ] speed    7 auto-pickup    8 solar module    F repair all", new Color(255, 232, 150))
        });
    }

    private void DrawLevelTutorialPrompts(SpriteBatch spriteBatch)
    {
        bool showThrottlePrompt = !_throttleTutorialCompleted;
        bool showFuelPrompt = !_fuelTutorialCompleted;
        bool showSolarInstallPrompt = _eventManager.ShouldShowSolarInstallButtonPrompt(_player);

        if (!showThrottlePrompt && !showFuelPrompt && !showSolarInstallPrompt)
        {
            return;
        }

        float y = 110f;
        if (showThrottlePrompt)
        {
            DrawCenteredTutorialPrompt(spriteBatch, "HOLD G + D TO PUSH THE THROTTLE", new Vector2(WorldConfig.ScreenWidth / 2f, y));
            y += 48f;
        }

        if (showFuelPrompt)
        {
            DrawCenteredTutorialPrompt(spriteBatch, "CARRY FUEL TO THE PORT, THEN JUMP TO PRESS THE BUTTON", new Vector2(WorldConfig.ScreenWidth / 2f, y));
            y += 48f;
        }

        if (showSolarInstallPrompt)
        {
            DrawCenteredTutorialPrompt(spriteBatch, "GO TO THE TOP LEVEL AND JUMP INTO THE BUTTON", new Vector2(WorldConfig.ScreenWidth / 2f, y));
        }
    }

    private static void DrawCenteredTutorialPrompt(SpriteBatch spriteBatch, string text, Vector2 centerTop)
    {
        SpriteFont font = AssetManager.ArialFont;
        Vector2 size = font.MeasureString(text) * TutorialPromptScale;
        Vector2 position = new(centerTop.X - size.X / 2f, centerTop.Y);

        spriteBatch.DrawString(font, text, position + new Vector2(2f, 2f), Color.Black * 0.78f, 0f, Vector2.Zero, TutorialPromptScale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, text, position, Color.White, 0f, Vector2.Zero, TutorialPromptScale, SpriteEffects.None, 0f);
    }

    private void DrawPauseOverlay(SpriteBatch spriteBatch)
    {
        if (!_isPaused)
        {
            return;
        }

        DrawPausePage(spriteBatch, "Esc  Resume", new List<HudLine>
        {
            new HudLine("Esc                    Pause / Resume", new Color(224, 244, 248)),
            new HudLine("WASD / Arrow Keys       Move", Color.White),
            new HudLine("Space / W / Up          Jump", Color.White),
            new HudLine("G                       Pick up, drop, press, advance dialogue", Color.White),
            new HudLine("G + D                   Push throttle", Color.White),
            new HudLine("Shift                   Toggle camera view", Color.White),
            new HudLine("F5                      Save game", Color.White),
            new HudLine("F3                      Toggle debug overlay", new Color(255, 232, 150))
        });
    }

    private void DrawRepairFlash(SpriteBatch spriteBatch)
    {
        if (_repairFlashTimer <= 0f)
        {
            return;
        }

        float alpha = MathHelper.Clamp(_repairFlashTimer / RepairFlashDuration, 0f, 1f) * 0.55f;
        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), new Rectangle(0, 0, WorldConfig.ScreenWidth, WorldConfig.ScreenHeight), Color.LimeGreen * alpha);
    }

    private void UpdateReturnCapsuleEvent(GameTime gameTime)
    {
        _eventManager.Update(gameTime, StoryDistance, _camera.ViewBounds, this);
        _camera.Update(gameTime, 0f);
    }

    private void SpawnInitialFuelBarrels(bool includeVehicleLeftBarrel = true)
    {
        if (_initialFuelBarrelsSpawned)
        {
            return;
        }

        if (includeVehicleLeftBarrel)
        {
            SpawnFuelBarrelAt(1800f);
        }

        SpawnFuelBarrelAt(2600f);
        SpawnFuelBarrelAt(3400f);
        _nextFuelSpawnX = MathF.Max(_nextFuelSpawnX, 4200f);
        _initialFuelBarrelsSpawned = true;
    }

    private void SpawnFuelBarrelsIfNeeded()
    {
        while (_nextFuelSpawnX < _worldScrollX + WorldConfig.WorldWidth + 1600f)
        {
            if (_random.NextDouble() < FuelBarrelSpawnChance)
            {
                SpawnFuelBarrelAt(_nextFuelSpawnX);
            }

            _nextFuelSpawnX += _random.Next(FuelBarrelSpawnMinSpacing, FuelBarrelSpawnMaxSpacing);
        }
    }

    private void SpawnFuelBarrelAt(float worldLocalX)
    {
        float groundY = WorldConfig.FakeGroundLocalRect.Y - WorldConfig.FuelBarrelSize.Y;
        FuelBarrelEntity barrel = new(PositionSpace.World, new Vector2(worldLocalX, groundY), _random.Next(0, 4));
        barrel.SetScene(this);
        barrel.Update(new GameTime());
        _fuelBarrels.Add(barrel);
    }

    private SaveData CaptureSaveData()
    {
        SaveData data = new()
        {
            WorldScrollX = _worldScrollX,
            NextFuelSpawnX = _nextFuelSpawnX,
            PreferOverviewView = _preferOverviewView,
            VehicleDiscovered = _vehicleDiscovered,
            InitialFuelBarrelsSpawned = _initialFuelBarrelsSpawned,
            RepairGunExplained = _repairGunExplained,
            PlayerPosition = VectorSaveData.FromVector2(_player.Position),
            Vehicle = _vehicle.CaptureSaveData(),
            Throttle = _throttle.CaptureSaveData(),
            FuelPortLit = _fuelPort.IsLit,
            AutoPickupEnabled = _autoPickupModule.IsEnabled,
            AutoPickupDamaged = _autoPickupModule.IsDamaged,
            SolarPanelInstalled = Game1.SolarPanelEnabled,
            SolarPanelHasEnergy = Game1.SolarPanelHasEnergy,
            SolarPanelIsDaytime = Game1.SolarPanelIsDaytime,
            SolarPanelBlockedByWeather = Game1.SolarPanelBlockedByWeather,
            FuelPortDamaged = _fuelPort.IsDamaged,
            SolarPanelDamaged = _solarPanel.IsDamaged,
            ThrottleDamaged = _throttle.IsDamaged,
            DamageFailureTimer = _damageFailureTimer,
            WorldEvents = _eventManager.CaptureSaveData()
        };

        for (int i = 0; i < _fuelBarrels.Count; i++)
        {
            FuelBarrelEntity barrel = _fuelBarrels[i];
            if (!barrel.IsActive)
            {
                continue;
            }

            if (barrel.IsCarried)
            {
                data.CarriedFuelBarrelIndex = data.FuelBarrels.Count;
            }

            data.FuelBarrels.Add(new FuelBarrelSaveData
            {
                Space = barrel.Space,
                LocalPosition = VectorSaveData.FromVector2(barrel.LocalPosition),
                OrientationQuarterTurns = barrel.OrientationQuarterTurns
            });
        }

        for (int i = 0; i < _repairGuns.Count; i++)
        {
            RepairGunEntity repairGun = _repairGuns[i];
            if (!repairGun.IsActive)
            {
                continue;
            }

            if (repairGun.IsCarried)
            {
                data.CarriedRepairGunIndex = data.RepairGuns.Count;
            }

            data.RepairGuns.Add(new RepairGunSaveData
            {
                Space = repairGun.Space,
                LocalPosition = VectorSaveData.FromVector2(repairGun.LocalPosition)
            });
        }

        return data;
    }

    private void RestoreSaveData(SaveData data)
    {
        _worldScrollX = data.WorldScrollX;
        _nextFuelSpawnX = data.NextFuelSpawnX;
        _preferOverviewView = data.PreferOverviewView;
        _vehicleDiscovered = true;
        _initialFuelBarrelsSpawned = data.InitialFuelBarrelsSpawned;
        _repairGunExplained = data.RepairGunExplained;
        Game1.SolarPanelEnabled = data.SolarPanelInstalled;
        Game1.RestoreSolarPanelEnergyState(data.SolarPanelIsDaytime, data.SolarPanelBlockedByWeather);

        if (data.PlayerPosition != null)
        {
            _player.SetPosition(data.PlayerPosition.ToVector2());
        }

        _vehicle.RestoreSaveData(data.Vehicle);
        _throttle.RestoreSaveData(data.Throttle);
        _fuelPort.SetLit(data.FuelPortLit);
        _damageFailureTimer = MathF.Max(0f, data.DamageFailureTimer);
        _autoPickupModule.SetEnabled(data.AutoPickupEnabled);
        if (data.AutoPickupDamaged)
        {
            _autoPickupModule.Damage();
        }
        else
        {
            _autoPickupModule.Repair();
        }
        _eventManager.RestoreSaveData(data.WorldEvents);

        if (data.FuelPortDamaged)
        {
            _fuelPort.Damage();
        }

        if (data.SolarPanelDamaged)
        {
            _solarPanel.Damage();
        }

        if (data.ThrottleDamaged)
        {
            _throttle.Damage();
        }

        _fuelBarrels.Clear();
        for (int i = 0; i < data.FuelBarrels.Count; i++)
        {
            FuelBarrelSaveData barrelData = data.FuelBarrels[i];
            FuelBarrelEntity barrel = new(barrelData.Space, barrelData.LocalPosition.ToVector2(), barrelData.OrientationQuarterTurns);
            barrel.SetScene(this);
            barrel.Update(new GameTime());

            if (i == data.CarriedFuelBarrelIndex)
            {
                barrel.PickUp(_player);
                _carriedFuelBarrel = barrel;
            }

            _fuelBarrels.Add(barrel);
        }

        _repairGuns.Clear();
        List<RepairGunSaveData> savedRepairGuns = data.RepairGuns ?? new List<RepairGunSaveData>();
        for (int i = 0; i < savedRepairGuns.Count; i++)
        {
            RepairGunSaveData repairGunData = savedRepairGuns[i];
            RepairGunEntity repairGun = new(repairGunData.Space, repairGunData.LocalPosition.ToVector2());
            repairGun.SetScene(this);
            repairGun.Update(new GameTime());

            if (i == data.CarriedRepairGunIndex)
            {
                repairGun.PickUp(_player);
                _carriedRepairGun = repairGun;
            }

            _repairGuns.Add(repairGun);
        }
    }

    private void CleanupFuelBarrels()
    {
        for (int i = _fuelBarrels.Count - 1; i >= 0; i--)
        {
            FuelBarrelEntity barrel = _fuelBarrels[i];
            if (!barrel.IsActive)
            {
                _fuelBarrels.RemoveAt(i);
                continue;
            }

            if (!barrel.IsCarried && barrel.GetBounds().Right < -200)
            {
                barrel.Deactivate();
                _fuelBarrels.RemoveAt(i);
            }
        }

        for (int i = _repairGuns.Count - 1; i >= 0; i--)
        {
            RepairGunEntity repairGun = _repairGuns[i];
            if (!repairGun.IsActive)
            {
                _repairGuns.RemoveAt(i);
                continue;
            }

            if (!repairGun.IsCarried && repairGun.GetBounds().Right < -200)
            {
                repairGun.Deactivate();
                _repairGuns.RemoveAt(i);
            }
        }
    }

    private void HandleFuelBarrelInteraction()
    {
        if (!ServiceLocator.Input.IsActionPressed(Action.Interact))
        {
            return;
        }

        if (_throttle.IsPlayerPushing)
        {
            return;
        }

        if (_carriedFuelBarrel != null)
        {
            if (_fuelPort.TryInsertBarrel(_player, _carriedFuelBarrel))
            {
                _carriedFuelBarrel.Deactivate();
                _carriedFuelBarrel = null;
                return;
            }

            DropCarriedFuelBarrel();
            return;
        }

        if (_carriedRepairGun != null)
        {
            DropCarriedRepairGun();
            return;
        }

        FuelBarrelEntity nearestBarrel = FindNearestPickup();
        if (nearestBarrel != null)
        {
            nearestBarrel.PickUp(_player);
            _carriedFuelBarrel = nearestBarrel;
            return;
        }

        RepairGunEntity nearestRepairGun = FindNearestRepairGunPickup();
        if (nearestRepairGun != null)
        {
            nearestRepairGun.PickUp(_player);
            _carriedRepairGun = nearestRepairGun;
            StartRepairGunExplanationIfNeeded();
        }
    }

    private FuelBarrelEntity FindNearestPickup()
    {
        Vector2 playerCenter = _player.GetBounds().Center.ToVector2();
        FuelBarrelEntity bestMatch = null;
        float bestDistance = 90f;

        foreach (FuelBarrelEntity barrel in _fuelBarrels)
        {
            if (barrel.IsCarried)
            {
                continue;
            }

            float distance = Vector2.Distance(playerCenter, barrel.GetBounds().Center.ToVector2());
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = barrel;
            }
        }

        return bestMatch;
    }

    private RepairGunEntity FindNearestRepairGunPickup()
    {
        Vector2 playerCenter = _player.GetBounds().Center.ToVector2();
        RepairGunEntity bestMatch = null;
        float bestDistance = 90f;

        foreach (RepairGunEntity repairGun in _repairGuns)
        {
            if (repairGun.IsCarried)
            {
                continue;
            }

            float distance = Vector2.Distance(playerCenter, repairGun.GetBounds().Center.ToVector2());
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = repairGun;
            }
        }

        return bestMatch;
    }

    private void TryRepairDamagedComponent()
    {
        if (_carriedRepairGun == null)
        {
            return;
        }

        Rectangle repairBounds = _carriedRepairGun.GetBounds();
        if (repairBounds == Rectangle.Empty)
        {
            return;
        }

        if (_fuelPort.IsDamaged && repairBounds.Intersects(_fuelPort.GetBounds()))
        {
            _fuelPort.Repair();
            TriggerRepairFlash();
            return;
        }

        if (_solarPanel.IsDamaged && repairBounds.Intersects(_solarPanel.GetBounds()))
        {
            _solarPanel.Repair();
            TriggerRepairFlash();
            return;
        }

        if (_throttle.IsDamaged && repairBounds.Intersects(_throttle.GetBounds()))
        {
            _throttle.Repair();
            TriggerRepairFlash();
            return;
        }

        if (_autoPickupModule.IsDamaged && repairBounds.Intersects(_autoPickupModule.GetBounds()))
        {
            _autoPickupModule.Repair();
            TriggerRepairFlash();
        }
    }

    private void TriggerRepairFlash()
    {
        _repairFlashTimer = RepairFlashDuration;
    }

    private void DebugRepairAllComponents()
    {
        _fuelPort.Repair();
        _solarPanel.Repair();
        _throttle.Repair();
        _autoPickupModule.Repair();
        _damageFailureTimer = 0f;
        TriggerRepairFlash();
    }

    private void UpdateDamageFailureState(float dt)
    {
        if (CountDamagedVehicleSystems() < DamageFailureThreshold)
        {
            _damageFailureTimer = 0f;
            return;
        }

        _damageFailureTimer += dt;
        if (IsDamageFailureDeathActive() && _damageFailureTimer >= DamageFailureDuration)
        {
            ChangeScene("gameOver");
        }
    }

    private void UpdateOffscreenFailureState(float dt)
    {
        if (IsPlayerVisibleOnScreen())
        {
            _offscreenFailureTimer = 0f;
            return;
        }

        _offscreenFailureTimer += dt;
        if (_offscreenFailureTimer >= OffscreenFailureDuration)
        {
            ChangeScene("gameOver");
        }
    }

    private bool IsPlayerVisibleOnScreen()
    {
        Rectangle playerBounds = _player.GetBounds();
        return playerBounds != Rectangle.Empty && playerBounds.Intersects(_camera.ViewBounds);
    }

    private int CountDamagedVehicleSystems()
    {
        int damagedCount = 0;

        if (_fuelPort.IsDamaged)
        {
            damagedCount++;
        }

        if (_solarPanel.IsDamaged)
        {
            damagedCount++;
        }

        if (_throttle.IsDamaged)
        {
            damagedCount++;
        }

        if (_autoPickupModule.IsDamaged)
        {
            damagedCount++;
        }

        return damagedCount;
    }

    private void DrawDamageFailureWarning(SpriteBatch spriteBatch)
    {
        if (CountDamagedVehicleSystems() < DamageFailureThreshold)
        {
            return;
        }

        if (!IsDamageFailureDeathActive())
        {
            return;
        }

        DrawFailureCountdownWarning(
            spriteBatch,
            "CRITICAL SYSTEM FAILURE",
            "Shutdown in ",
            _damageFailureTimer,
            DamageFailureDuration,
            96f,
            2f,
            1.5f);
    }

    private void DrawOffscreenFailureWarning(SpriteBatch spriteBatch)
    {
        if (_offscreenFailureTimer <= 0f || IsPlayerVisibleOnScreen())
        {
            return;
        }

        DrawFailureCountdownWarning(
            spriteBatch,
            "STAY NEAR THE ROVER",
            "Danger in ",
            _offscreenFailureTimer,
            OffscreenFailureDuration,
            176f,
            1.7f,
            1.25f);
    }

    private void DrawFailureCountdownWarning(
        SpriteBatch spriteBatch,
        string title,
        string countdownPrefix,
        float elapsedSeconds,
        float durationSeconds,
        float titleY,
        float titleScale,
        float countdownScale)
    {
        float pulse = 0.5f + 0.5f * MathF.Sin(elapsedSeconds * 10f);
        float urgency = MathHelper.Clamp(elapsedSeconds / durationSeconds, 0f, 1f);
        float alpha = MathHelper.Lerp(0.12f, 0.45f, urgency) * pulse;
        Rectangle bounds = new(0, 0, WorldConfig.ScreenWidth, WorldConfig.ScreenHeight);
        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), bounds, Color.Red * alpha);

        int secondsRemaining = (int)MathF.Ceiling(MathF.Max(0f, durationSeconds - elapsedSeconds));
        DrawCenteredHudText(spriteBatch, title, new Vector2(WorldConfig.ScreenWidth / 2f, titleY), Color.White, titleScale);
        DrawCenteredHudText(spriteBatch, countdownPrefix + secondsRemaining + "s", new Vector2(WorldConfig.ScreenWidth / 2f, titleY + 34f), Color.Yellow, countdownScale);
    }

    private bool IsDamageFailureDeathActive()
    {
        return StoryDistance < DamageFailureDeathMaxDistance;
    }

    private static void DrawCenteredHudText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale)
    {
        Vector2 textSize = AssetManager.ArialFont.MeasureString(text) * scale;
        Vector2 drawPosition = new(position.X - textSize.X / 2f, position.Y);
        Vector2 shadowOffset = new(2f, 2f);

        spriteBatch.DrawString(AssetManager.ArialFont, text, drawPosition + shadowOffset, Color.Black * 0.85f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(AssetManager.ArialFont, text, drawPosition, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void BreakRepairGuns()
    {
        if (_carriedRepairGun != null)
        {
            _carriedRepairGun.Deactivate();
            _carriedRepairGun = null;
        }

        foreach (RepairGunEntity repairGun in _repairGuns)
        {
            repairGun.Deactivate();
        }
    }

    private void StartRepairGunExplanationIfNeeded()
    {
        if (_repairGunExplained)
        {
            return;
        }

        _repairGunExplained = true;
        StartDialogue(new[]
        {
            new DialogueLine
            {
                Speaker = "houston",
                Text = "That blue tool is a repair gun. Touch a broken module with it and it should reboot the damaged system."
            },
            new DialogueLine
            {
                Speaker = "houston",
                Text = "Start with the auto pickup module if you can. It will save you a lot of fuel-can hauling."
            }
        });
    }

    private void DropCarriedFuelBarrel()
    {
        Vector2 worldDropPosition = GetSafeDropPosition(WorldConfig.FuelBarrelSize);
        PositionSpace targetSpace = IsPlayerInsideVehicle(_player) ? PositionSpace.Vehicle : PositionSpace.World;
        Vector2 targetLocalPosition = WorldToLocal(targetSpace, worldDropPosition);

        _carriedFuelBarrel.Drop(targetSpace, targetLocalPosition);
        _carriedFuelBarrel = null;
    }

    private void DropCarriedRepairGun()
    {
        Vector2 worldDropPosition = GetSafeDropPosition(WorldConfig.RepairGunSize);
        PositionSpace targetSpace = IsPlayerInsideVehicle(_player) ? PositionSpace.Vehicle : PositionSpace.World;
        Vector2 targetLocalPosition = WorldToLocal(targetSpace, worldDropPosition);

        _carriedRepairGun.Drop(targetSpace, targetLocalPosition);
        _carriedRepairGun = null;
    }

    private Vector2 GetSafeDropPosition(Point itemSize)
    {
        Vector2 carryAnchor = _player.GetCarryAnchor(itemSize);
        return IsDropPositionBlocked(carryAnchor, itemSize) ? _player.Position : carryAnchor;
    }

    private bool IsDropPositionBlocked(Vector2 worldPosition, Point itemSize)
    {
        Rectangle itemBounds = new(
            (int)MathF.Round(worldPosition.X),
            (int)MathF.Round(worldPosition.Y),
            itemSize.X,
            itemSize.Y);

        foreach (Rectangle solid in GetSolidRectangles())
        {
            if (solid != Rectangle.Empty && itemBounds.Intersects(solid))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateCameraMode()
    {
        if (ShouldUseOverviewCamera())
        {
            _camera.ShowOverview();
        }
        else
        {
            _camera.ShowCloseVehicleView();
        }
    }

    private bool ShouldUseOverviewCamera()
    {
        return !IsPlayerInsideVehicle(_player) || _preferOverviewView;
    }

    private void HandleDebugSpeedControls(float dt)
    {
        float delta = 0f;

        if (ServiceLocator.Input.IsActionDown(Action.DebugSpeedIncrease))
        {
            delta += DebugSpeedAdjustPerSecond * dt;
        }

        if (ServiceLocator.Input.IsActionDown(Action.DebugSpeedDecrease))
        {
            delta -= DebugSpeedAdjustPerSecond * dt;
        }

        if (delta != 0f)
        {
            _vehicle.AdjustSpeed(delta);
        }
    }

}
