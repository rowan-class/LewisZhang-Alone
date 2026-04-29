using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class LevelScene : Scene
{
    private const float DebugSpeedAdjustPerSecond = 240f;

    private readonly Camera2D _camera = new();
    private readonly VehicleEntity _vehicle = new();
    private readonly Player _player = new(WorldConfig.PlayerStartPosition);
    private readonly GroundEntity _ground = new();
    private readonly List<FuelBarrelEntity> _fuelBarrels = new();
    private readonly Random _random = new(7);

    private float _worldScrollX;
    private float _nextFuelSpawnX = 2400f;
    private bool _preferOverviewView;
    private FuelBarrelEntity _carriedFuelBarrel;

    public LevelScene()
    {
        _vehicle.SetScene(this);
        _player.SetScene(this);
        _ground.SetScene(this);
        _ground.Update(new GameTime());

        SpawnInitialFuelBarrels();
        UpdateCameraMode();
    }

    public float WorldScrollX => _worldScrollX;
    public float VehicleSpeed => _vehicle.Speed;
    public VehicleEntity Vehicle => _vehicle;

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (ServiceLocator.Input.IsActionPressed(Action.ToggleVehiclePower))
        {
            _vehicle.TogglePower();
        }

        if (ServiceLocator.Input.IsActionPressed(Action.ToggleCameraView))
        {
            _preferOverviewView = !_preferOverviewView;
        }

        _vehicle.Update(gameTime);

        if (Game1.Debug)
        {
            HandleDebugSpeedControls(dt);
        }

        _worldScrollX += _vehicle.Speed * dt;

        _ground.Update(gameTime);
        SpawnFuelBarrelsIfNeeded();
        UpdateFuelBarrels(gameTime);
        _player.Update(gameTime);
        HandleFuelBarrelInteraction();
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
        _ground.Draw(spriteBatch);
        _vehicle.Draw(spriteBatch);
        DrawFuelBarrels(spriteBatch, carriedOnly: false);
        DrawFuelBarrels(spriteBatch, carriedOnly: true);
        _player.Draw(spriteBatch);

        if (Game1.Debug)
        {
            DrawDebugRectangles(spriteBatch, GetDebugRectangles());
            DrawDebugRectangle(spriteBatch, _vehicle.CabinBoundsWorld, Color.LimeGreen);
        }

        spriteBatch.End();

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawHud(spriteBatch);
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
    }

    public bool IsPlayerInsideVehicle(Player player)
    {
        return _vehicle.IsInsideCabin(player.GetBounds());
    }

    public bool IsPlayerAttachedToVehicle(Player player)
    {
        return IsPlayerInsideVehicle(player);
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
        string carryingState = _carriedFuelBarrel == null ? "None" : "Fuel Barrel";

        spriteBatch.DrawString(AssetManager.ArialFont, "E Toggle Power", new Vector2(20, 20), Color.White);
        spriteBatch.DrawString(AssetManager.ArialFont, "Shift Toggle Camera", new Vector2(20, 48), Color.White);
        spriteBatch.DrawString(AssetManager.ArialFont, "WASD / Arrows Move, Space Jump, G Pick / Drop", new Vector2(20, 76), Color.White);
        spriteBatch.DrawString(AssetManager.ArialFont, "Vehicle: " + powerState + "  Speed: " + _vehicle.Speed.ToString("0.0"), new Vector2(20, 116), Color.White);
        spriteBatch.DrawString(AssetManager.ArialFont, "Camera: " + cameraState + "  Carrying: " + carryingState, new Vector2(20, 144), Color.White);
        spriteBatch.DrawString(AssetManager.ArialFont, "Player State: " + _player.CurrentState, new Vector2(20, 172), Color.White);
        if (Game1.Debug)
        {
            spriteBatch.DrawString(AssetManager.ArialFont, "Debug Speed: [ decrease   ] increase", new Vector2(20, 200), Color.Yellow);
        }
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
    }

    private void HandleFuelBarrelInteraction()
    {
        if (!ServiceLocator.Input.IsActionPressed(Action.Interact))
        {
            return;
        }

        if (_carriedFuelBarrel != null)
        {
            DropCarriedFuelBarrel();
            return;
        }

        FuelBarrelEntity nearestBarrel = FindNearestPickup();
        if (nearestBarrel == null)
        {
            return;
        }

        nearestBarrel.PickUp(_player);
        _carriedFuelBarrel = nearestBarrel;
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

    private void DropCarriedFuelBarrel()
    {
        Vector2 worldDropPosition = _player.GetCarryAnchor(WorldConfig.FuelBarrelSize);
        PositionSpace targetSpace = IsPlayerInsideVehicle(_player) ? PositionSpace.Vehicle : PositionSpace.World;
        Vector2 targetLocalPosition = WorldToLocal(targetSpace, worldDropPosition);

        _carriedFuelBarrel.Drop(targetSpace, targetLocalPosition);
        _carriedFuelBarrel = null;
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
