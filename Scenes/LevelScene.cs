using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class LevelScene : Scene
{
    private const float DebugSpeedAdjustPerSecond = 240f;
    private const float RepairFlashDuration = 0.25f;

    private readonly Camera2D _camera = new();
    private readonly WorldEventManager _eventManager = new();
    private readonly VehicleEntity _vehicle = new();
    private readonly ThrottleEntity _throttle;
    private readonly FuelDisplayEntity _fuelDisplay;
    private readonly FuelPortEntity _fuelPort;
    private readonly FuelButtonModuleEntity _fuelButton;
    private readonly HandbrakeButtonEntity _handbrakeButton;
    private readonly SolarButtonEntity _solarButton;
    private readonly AutoPickupModuleEntity _autoPickupModule;
    private readonly SolarPanelEntity _solarPanel;
    private readonly Player _player = new(WorldConfig.PlayerStartPosition);
    private readonly GroundEntity _ground = new();
    private readonly List<FuelBarrelEntity> _fuelBarrels = new();
    private readonly List<RepairGunEntity> _repairGuns = new();
    private readonly Random _random = new(7);

    private float _worldScrollX;
    private float _nextFuelSpawnX = 2400f;
    private float _repairFlashTimer;
    private bool _preferOverviewView;
    private FuelBarrelEntity _carriedFuelBarrel;
    private RepairGunEntity _carriedRepairGun;

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

        if (saveData == null)
        {
            SpawnInitialFuelBarrels();
        }
        else
        {
            RestoreSaveData(saveData);
        }

        UpdateCameraMode();
    }

    public float WorldScrollX => _worldScrollX;
    public float VehicleSpeed => _vehicle.Speed;
    public VehicleEntity Vehicle => _vehicle;
    public bool IsPlayerCarryingFuelBarrel => _carriedFuelBarrel != null;
    public bool IsPlayerCarryingItem => _carriedFuelBarrel != null || _carriedRepairGun != null;
    public Player Player => _player;
    public Rectangle PlayerBounds => _player.GetBounds();
    public bool IsThrottleLockedByEvent => _eventManager.IsThrottleLocked(_worldScrollX);

    public IEnumerable<FuelBarrelEntity> GetFuelBarrels()
    {
        return _fuelBarrels;
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

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

        _ground.Update(gameTime);
        SpawnFuelBarrelsIfNeeded();
        _player.Update(gameTime);
        _solarButton.Update(gameTime);
        _handbrakeButton.Update(gameTime);
        _throttle.Update(gameTime);
        _vehicle.Update(gameTime);
        _fuelPort.Update(gameTime);
        _fuelButton.Update(gameTime);
        _solarPanel.Update(gameTime);

        if (Game1.Debug)
        {
            HandleDebugSpeedControls(dt);
        }

        _eventManager.Update(gameTime, _worldScrollX, _camera.ViewBounds, this);
        _worldScrollX += _vehicle.Speed * dt;

        _fuelDisplay.Update(gameTime);
        UpdateFuelBarrels(gameTime);
        UpdateRepairGuns(gameTime);
        TryRepairDamagedComponent();
        HandleFuelBarrelInteraction();
        _autoPickupModule.Update(gameTime);
        _repairFlashTimer = MathF.Max(0f, _repairFlashTimer - dt);
        UpdateCameraMode();
        _camera.Update(gameTime, _vehicle.Speed);
        CleanupFuelBarrels();

        if (_player.GetBounds().Top > WorldConfig.OverviewViewBounds.Bottom + 200 || _player.GetBounds().Right < WorldConfig.OverviewViewBounds.Left - 200)
        {
            ChangeScene("level1");
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix(), samplerState: SamplerState.PointClamp);
        DrawBackground(spriteBatch);
        _eventManager.DrawWorld(spriteBatch, _camera.ViewBounds);
        _ground.Draw(spriteBatch);
        _vehicle.Draw(spriteBatch);
        _throttle.Draw(spriteBatch);
        _fuelDisplay.Draw(spriteBatch);
        _fuelPort.Draw(spriteBatch);
        _fuelButton.Draw(spriteBatch);
        _handbrakeButton.Draw(spriteBatch);
        _solarButton.Draw(spriteBatch);
        _autoPickupModule.Draw(spriteBatch);
        _solarPanel.Draw(spriteBatch);
        DrawFuelBarrels(spriteBatch, carriedOnly: false);
        DrawRepairGuns(spriteBatch, carriedOnly: false);
        DrawFuelBarrels(spriteBatch, carriedOnly: true);
        DrawRepairGuns(spriteBatch, carriedOnly: true);
        _player.Draw(spriteBatch);
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
        }
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

    private void DrawFuelBarrels(SpriteBatch spriteBatch, bool carriedOnly)
    {
        foreach (FuelBarrelEntity barrel in _fuelBarrels)
        {
            if (barrel.IsActive && barrel.IsCarried == carriedOnly)
            {
                barrel.Draw(spriteBatch);
            }
        }
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

    private void DrawRepairGuns(SpriteBatch spriteBatch, bool carriedOnly)
    {
        foreach (RepairGunEntity repairGun in _repairGuns)
        {
            if (repairGun.IsActive && repairGun.IsCarried == carriedOnly)
            {
                repairGun.Draw(spriteBatch);
            }
        }
    }

    private void DrawBackground(SpriteBatch spriteBatch)
    {
        Texture2D background = AssetManager.GetTexture(Art.Background);
        float wrappedOffset = _worldScrollX % background.Width;
        Rectangle firstBackground = new(-(int)wrappedOffset, 0, background.Width, WorldConfig.WorldHeight);
        Rectangle secondBackground = new(-(int)wrappedOffset + background.Width, 0, background.Width, WorldConfig.WorldHeight);

        spriteBatch.Draw(background, firstBackground, Color.White);
        spriteBatch.Draw(background, secondBackground, Color.White);
    }

    private void DrawHud(SpriteBatch spriteBatch)
    {
        string powerState = _vehicle.Powered ? "Powered" : "No Power";
        string cameraState = ShouldUseOverviewCamera() ? "Overview" : "Interior";
        string carryingState = _carriedFuelBarrel != null ? "Fuel Barrel" : _carriedRepairGun != null ? "Repair Gun" : "None";

        spriteBatch.DrawString(AssetManager.ArialFont, "G + D Push Throttle", new Vector2(20, 20), Color.White);
        spriteBatch.DrawString(AssetManager.ArialFont, "Shift Toggle Camera", new Vector2(20, 48), Color.White);
        spriteBatch.DrawString(AssetManager.ArialFont, "WASD / Arrows Move, Space Jump, G Pick / Drop, F5 Save", new Vector2(20, 76), Color.White);

        if (Game1.Debug)
        {
            spriteBatch.DrawString(AssetManager.ArialFont, "Vehicle: " + powerState + "  Speed: " + _vehicle.Speed.ToString("0.0"), new Vector2(20, 116), Color.White);
            spriteBatch.DrawString(AssetManager.ArialFont, "Camera: " + cameraState + "  Carrying: " + carryingState, new Vector2(20, 144), Color.White);
            spriteBatch.DrawString(AssetManager.ArialFont, "Player State: " + _player.CurrentState, new Vector2(20, 172), Color.White);
            spriteBatch.DrawString(AssetManager.ArialFont, "Distance: " + _worldScrollX.ToString("0.0"), new Vector2(20, 200), Color.LightGreen);
            spriteBatch.DrawString(AssetManager.ArialFont, "Debug Speed: [ decrease   ] increase", new Vector2(20, 228), Color.Yellow);
            spriteBatch.DrawString(AssetManager.ArialFont, "Auto Pickup: " + (_autoPickupModule.IsEnabled ? "On" : "Off") + "  " + (_autoPickupModule.IsDamaged ? "Broken" : "OK") + "  (7 Toggle)", new Vector2(20, 256), Color.Yellow);
            string solarCondition = Game1.SolarPanelBlockedByWeather ? "Sandstorm" : Game1.SolarPanelIsDaytime ? "Day" : "Night";
            spriteBatch.DrawString(AssetManager.ArialFont, "Solar: " + (Game1.SolarPanelEnabled ? "On" : "Off") + "  " + solarCondition + "  (8 Toggle)", new Vector2(20, 284), Color.Cyan);
            spriteBatch.DrawString(AssetManager.ArialFont, "Event: " + _eventManager.GetDebugStatus(), new Vector2(20, 312), Color.Orange);
            spriteBatch.DrawString(AssetManager.ArialFont, "Damage: Fuel " + (_fuelPort.IsDamaged ? "Broken" : "OK") + "  Solar " + (_solarPanel.IsDamaged ? "Broken" : "OK") + "  Throttle " + (_throttle.IsDamaged ? "Broken" : "OK") + "  Pickup " + (_autoPickupModule.IsDamaged ? "Broken" : "OK"), new Vector2(20, 340), Color.OrangeRed);
        }
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

    private void SpawnInitialFuelBarrels()
    {
        SpawnFuelBarrelAt(1800f);
        SpawnFuelBarrelAt(2600f);
        SpawnFuelBarrelAt(3400f);
    }

    private void SpawnFuelBarrelsIfNeeded()
    {
        while (_nextFuelSpawnX < _worldScrollX + WorldConfig.WorldWidth + 1600f)
        {
            SpawnFuelBarrelAt(_nextFuelSpawnX);
            _nextFuelSpawnX += _random.Next(520, 920);
        }
    }

    private void SpawnFuelBarrelAt(float worldLocalX)
    {
        float groundY = WorldConfig.FakeGroundLocalRect.Y - WorldConfig.FuelBarrelSize.Y;
        FuelBarrelEntity barrel = new(PositionSpace.World, new Vector2(worldLocalX, groundY));
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
                LocalPosition = VectorSaveData.FromVector2(barrel.LocalPosition)
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
        Game1.SolarPanelEnabled = data.SolarPanelInstalled;
        Game1.RestoreSolarPanelEnergyState(data.SolarPanelIsDaytime, data.SolarPanelBlockedByWeather);

        if (data.PlayerPosition != null)
        {
            _player.SetPosition(data.PlayerPosition.ToVector2());
        }

        _vehicle.RestoreSaveData(data.Vehicle);
        _throttle.RestoreSaveData(data.Throttle);
        _fuelPort.SetLit(data.FuelPortLit);
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
            FuelBarrelEntity barrel = new(barrelData.Space, barrelData.LocalPosition.ToVector2());
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
