using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public enum WalkingSceneMode
{
    Intro,
    Final
}

public class WalkingScene : Scene, IPlayerScene
{
    private const string ProjectFileName = "LewisZhang-Alone.csproj";
    private const string DistanceTrigger = "distance";
    private const string SceneStartTrigger = "scene_start";
    private const string VehicleReachedTrigger = "vehicle_reached";
    private const float CameraFollowAnchorX = 0.34f;
    private const float CameraSharpness = 7f;
    private const float IntroStormDuration = 7f;
    private const float FinalStormDuration = 5f;
    private const float StormScrollSpeed = 760f;
    private const float StormSheetScreenWidth = 1800f;
    private const float DamageBlinkInterval = 0.18f;
    private const int IntroWorldWidth = 5600;
    private const int FinalWorldWidth = 5200;
    private const int WalkingCloseViewWidth = WorldConfig.ScreenWidth;
    private const int WalkingCloseViewHeight = WorldConfig.ScreenHeight;
    private const int GroundHeight = 160;

    private static readonly Vector2 IntroPlayerStart = new(160f, WorldConfig.FakeGroundLocalRect.Y - 48f);
    private static readonly Vector2 IntroVehiclePosition = new(3700f, WorldConfig.FakeGroundLocalRect.Y - 540f);
    private static readonly Vector2 FinalVehiclePosition = new(180f, WorldConfig.FakeGroundLocalRect.Y - 540f);
    private static readonly Vector2 FinalPlayerStart = new(160f, WorldConfig.FakeGroundLocalRect.Y - 48f);
    private static readonly Vector2 FinalCapsulePosition = new(3700f, WorldConfig.FakeGroundLocalRect.Y - 256f);
    private static readonly Rectangle[] IntroTerrainRectangles =
    {
        new Rectangle(508, 1278, 199, 97),
        new Rectangle(804, 1278, 87, 139),
        new Rectangle(891, 1318, 87, 99),
        new Rectangle(1160, 1374, 55, 36),
    };
    private static readonly Rectangle[] FinalTerrainRectangles =
    {
    };
    private static readonly Rectangle[] FinalVehiclePlatformLocalRectangles =
    {
        new Rectangle(24, 422, 900, 16),
        new Rectangle(90, 326, 320, 12),
        new Rectangle(510, 326, 400, 12),
        new Rectangle(90, 204, 320, 12),
        new Rectangle(625, 204, 300, 12),
        new Rectangle(60, 64, 820, 20),
    };

    private readonly WalkingSceneMode _mode;
    private readonly TextBoxEntity _textBox = new();
    private readonly Player _player;
    private readonly VehicleEntity _introVehicle;
    private readonly VehicleEntity _finalVehicle;
    private readonly List<WalkingDialogueDefinition> _dialogueDefinitions;
    private readonly HashSet<string> _triggeredDialogueIds = new();
    private readonly RockObstacle[] _rockObstacles;
    private readonly int _worldWidth;
    private readonly Rectangle _movementBounds;
    private readonly LevelToFinalWalkTransition _finalTransition;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private float _cameraX;
    private float _stormTimer;
    private float _stormOffset;
    private float _moduleDamageBlinkTimer;
    private string _pendingScene;
    private bool _isLaunching;
    private bool _isPaused;
    private float _capsuleUpperOffsetY;

    public WalkingScene(WalkingSceneMode mode)
    {
        _mode = mode;
        _worldWidth = mode == WalkingSceneMode.Intro ? IntroWorldWidth : FinalWorldWidth;
        _movementBounds = new Rectangle(0, 0, _worldWidth, WorldConfig.WorldHeight);
        _stormTimer = mode == WalkingSceneMode.Intro ? IntroStormDuration : FinalStormDuration;
        _dialogueDefinitions = LoadDialogueDefinitions(mode);
        _rockObstacles = CreateWalkingRockObstacles(mode);

        if (mode == WalkingSceneMode.Intro)
        {
            Game1.SolarPanelEnabled = false;
            Game1.ResetSolarPanelEnergyState();
            _player = new Player(IntroPlayerStart);
            _introVehicle = new VehicleEntity();
            _introVehicle.SetPosition(IntroVehiclePosition);
            _introVehicle.SetScene(this);
            _finalVehicle = null;
            _finalTransition = null;
        }
        else
        {
            _introVehicle = null;
            LevelToFinalWalkTransition transition = SceneTransitionContext.ConsumeLevelToFinalWalk();
            _finalTransition = transition ?? new LevelToFinalWalkTransition { FuelRatio = VehicleResources.InitialFuel / VehicleResources.MaxFuel };
            _moduleDamageBlinkTimer = _finalTransition.ModuleDamageBlinkTimer;
            _finalVehicle = new VehicleEntity();
            _finalVehicle.SetPosition(FinalVehiclePosition);
            _finalVehicle.SetScene(this);
            _player = new Player(GetFinalPlayerStartPosition(transition));

            StartTriggeredDialogue(SceneStartTrigger);
        }

        _player.SetScene(this);
        UpdateCamera(1f / 60f);
    }

    public bool IsPlayerCarryingItem => false;
    public bool IsPlayerInputLocked => _textBox.IsActiveDialogue || _isLaunching;
    public bool CanPlayerMoveLeft => true;
    public float VehicleSpeed => 0f;
    public Rectangle PlayerMovementBounds => _movementBounds;

    public bool IsPlayerAttachedToVehicle(Player player)
    {
        return false;
    }

    public IEnumerable<Rectangle> GetSolidRectangles()
    {
        yield return GetGroundBounds();

        foreach (Rectangle rect in GetWalkingTerrainRectangles())
        {
            yield return rect;
        }

        foreach (RockObstacle obstacle in _rockObstacles)
        {
            yield return GetWalkingRockCollisionBounds(obstacle);
        }

        if (_mode == WalkingSceneMode.Intro)
        {
            yield return GetIntroVehicleBarrierBounds();
        }

        if (_mode == WalkingSceneMode.Final)
        {
            foreach (Rectangle rect in GetFinalVehiclePlatformWorldRectangles())
            {
                yield return rect;
            }

            yield return GetCapsuleLowerCollisionBounds();
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

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        UpdateStorm(dt);
        UpdateModuleDamageBlink(dt);

        if (_textBox.IsActiveDialogue)
        {
            _textBox.Update(gameTime);
            if (!_textBox.IsActiveDialogue && !string.IsNullOrEmpty(_pendingScene))
            {
                ChangeScene(_pendingScene);
            }

            UpdateCamera(dt);
            return;
        }

        if (_isLaunching)
        {
            UpdateLaunch(dt);
            UpdateCamera(dt);
            return;
        }

        _player.Update(gameTime);

        if (_mode == WalkingSceneMode.Intro)
        {
            UpdateIntroTriggers();
        }
        else
        {
            UpdateFinalTriggers();
        }

        UpdateCamera(dt);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(transformMatrix: GetViewMatrix(), samplerState: SamplerState.PointClamp);
        DrawBackground(spriteBatch);
        DrawGround(spriteBatch);

        if (_mode == WalkingSceneMode.Intro)
        {
            _introVehicle.Draw(spriteBatch);
            DrawIntroVehicleModules(spriteBatch);
        }
        else
        {
            _finalVehicle.Draw(spriteBatch);
            DrawFinalVehicleModules(spriteBatch);
            DrawCapsule(spriteBatch);
        }

        DrawWalkingRockObstacles(spriteBatch);

        if (!_isLaunching)
        {
            _player.Draw(spriteBatch);
        }

        if (Game1.Debug)
        {
            DrawDebugRectangles(spriteBatch, GetDebugRectangles());
        }

        spriteBatch.End();

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawStormOverlay(spriteBatch);
        DrawHud(spriteBatch);
        if (!_isPaused)
        {
            DrawPauseHint(spriteBatch);
        }
        _textBox.Draw(spriteBatch);
        DrawPauseOverlay(spriteBatch);
        spriteBatch.End();
    }

    protected override IEnumerable<Rectangle> GetDebugRectangles()
    {
        yield return GetGroundBounds();

        foreach (Rectangle rect in _player.GetDebugRectangles())
        {
            yield return rect;
        }

        if (_mode == WalkingSceneMode.Intro)
        {
            foreach (Rectangle rect in _introVehicle.GetDebugRectangles())
            {
                yield return rect;
            }

            yield return GetIntroVehicleBarrierBounds();
        }
        else
        {
            foreach (Rectangle rect in GetFinalVehiclePlatformWorldRectangles())
            {
                yield return rect;
            }

            yield return GetCapsuleLowerCollisionBounds();
            yield return GetCapsuleEntranceBounds();
        }

        foreach (Rectangle rect in GetWalkingTerrainRectangles())
        {
            yield return rect;
        }

        foreach (RockObstacle obstacle in _rockObstacles)
        {
            yield return GetWalkingRockCollisionBounds(obstacle);
        }
    }

    private void UpdateIntroTriggers()
    {
        float walkedDistance = MathF.Max(0f, _player.Position.X - IntroPlayerStart.X);

        if (StartDistanceDialogue(walkedDistance))
        {
            return;
        }

        if (_player.GetBounds().Right >= GetIntroVehicleBarrierBounds().Left)
        {
            SceneTransitionContext.IntroToLevel = new IntroToLevelTransition
            {
                PlayerVehicleLocalPosition = _player.Position - IntroVehiclePosition
            };
            _pendingScene = "level1";
            if (!StartTriggeredDialogue(VehicleReachedTrigger))
            {
                ChangeScene(_pendingScene);
            }
        }
    }

    private bool StartDistanceDialogue(float walkedDistance)
    {
        foreach (WalkingDialogueDefinition definition in _dialogueDefinitions)
        {
            if (!string.Equals(definition.Trigger, DistanceTrigger, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (walkedDistance >= definition.TriggerDistance && StartDialogue(definition))
            {
                return true;
            }
        }

        return false;
    }

    private bool StartTriggeredDialogue(string trigger)
    {
        foreach (WalkingDialogueDefinition definition in _dialogueDefinitions)
        {
            if (string.Equals(definition.Trigger, trigger, StringComparison.OrdinalIgnoreCase)
                && StartDialogue(definition))
            {
                return true;
            }
        }

        return false;
    }

    private bool StartDialogue(WalkingDialogueDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || _triggeredDialogueIds.Contains(definition.Id))
        {
            return false;
        }

        _triggeredDialogueIds.Add(definition.Id);
        _textBox.StartDialogue(definition.Dialogue);
        return _textBox.IsActiveDialogue;
    }

    private void UpdateFinalTriggers()
    {
        if (_player.GetBounds().Intersects(GetCapsuleEntranceBounds()))
        {
            _isLaunching = true;
            _capsuleUpperOffsetY = 0f;
            _player.Deactivate();
        }
    }

    private void UpdateLaunch(float dt)
    {
        _capsuleUpperOffsetY -= WorldConfig.ReturnCapsuleLaunchSpeed * dt;
        if (GetCapsuleUpperBounds().Bottom < GetCameraY() - 32f)
        {
            ChangeScene("end");
        }
    }

    private void UpdateStorm(float dt)
    {
        if (_stormTimer <= 0f)
        {
            return;
        }

        _stormTimer = MathF.Max(0f, _stormTimer - dt);
        _stormOffset += StormScrollSpeed * dt;
    }

    private void UpdateModuleDamageBlink(float dt)
    {
        if (_mode != WalkingSceneMode.Final)
        {
            return;
        }

        _moduleDamageBlinkTimer += dt;
    }

    private void UpdateCamera(float dt)
    {
        int viewWidth = GetCameraViewWidth();
        float targetX = MathHelper.Clamp(
            _player.Position.X - viewWidth * CameraFollowAnchorX,
            0f,
            MathF.Max(0f, _worldWidth - viewWidth));

        float smoothing = dt <= 0f ? 1f : 1f - MathF.Exp(-CameraSharpness * dt);
        _cameraX = MathHelper.Lerp(_cameraX, targetX, smoothing);
    }

    private Matrix GetViewMatrix()
    {
        float scaleX = (float)WorldConfig.ScreenWidth / GetCameraViewWidth();
        float scaleY = (float)WorldConfig.ScreenHeight / GetCameraViewHeight();

        return Matrix.CreateTranslation(-_cameraX, -GetCameraY(), 0f)
            * Matrix.CreateScale(scaleX, scaleY, 1f);
    }

    private float GetCameraY()
    {
        return WorldConfig.FakeGroundLocalRect.Y - GetCameraViewHeight() + 120f;
    }

    private int GetCameraViewWidth()
    {
        return _mode == WalkingSceneMode.Intro ? WorldConfig.OverviewWidth : WalkingCloseViewWidth;
    }

    private int GetCameraViewHeight()
    {
        return _mode == WalkingSceneMode.Intro ? WorldConfig.OverviewHeight : WalkingCloseViewHeight;
    }

    private Rectangle GetGroundBounds()
    {
        return new Rectangle(0, WorldConfig.FakeGroundLocalRect.Y, _worldWidth, GroundHeight);
    }

    private IEnumerable<Rectangle> GetWalkingTerrainRectangles()
    {
        Rectangle[] terrain = _mode == WalkingSceneMode.Intro
            ? IntroTerrainRectangles
            : FinalTerrainRectangles;

        foreach (Rectangle rect in terrain)
        {
            yield return rect;
        }
    }

    private void DrawWalkingRockObstacles(SpriteBatch spriteBatch)
    {
        foreach (RockObstacle obstacle in _rockObstacles)
        {
            DrawRockObstacle(spriteBatch, obstacle.Art, GetWalkingRockBounds(obstacle));
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

    private static Rectangle GetWalkingRockBounds(RockObstacle obstacle)
    {
        return new Rectangle(
            (int)MathF.Round(obstacle.WorldLocalX),
            (int)MathF.Round(WorldConfig.FakeGroundLocalRect.Y + 50f - obstacle.Size.Y),
            obstacle.Size.X,
            obstacle.Size.Y);
    }

    private static Rectangle GetWalkingRockCollisionBounds(RockObstacle obstacle)
    {
        Rectangle bounds = GetWalkingRockBounds(obstacle);
        return new Rectangle(
            bounds.X + 16,
            bounds.Y + 8,
            Math.Max(1, bounds.Width - 32),
            Math.Max(1, bounds.Height - 8));
    }

    private static RockObstacle[] CreateWalkingRockObstacles(WalkingSceneMode mode)
    {
        Random random = new(mode == WalkingSceneMode.Intro ? 101 : 151);
        List<RockObstacle> obstacles = new();
        float x = mode == WalkingSceneMode.Intro ? 1320f : 1320f;
        float maxX = mode == WalkingSceneMode.Intro ? 3300f : 3250f;

        while (x < maxX)
        {
            x += random.Next(320, 560);
            RockObstacle obstacle = CreateRandomWalkingRock(random, x);

            if (mode == WalkingSceneMode.Intro && RangesOverlap(obstacle.WorldLocalX, obstacle.WorldLocalX + obstacle.Size.X, 3420f, 4300f))
            {
                continue;
            }

            if (mode == WalkingSceneMode.Final && RangesOverlap(obstacle.WorldLocalX, obstacle.WorldLocalX + obstacle.Size.X, 3400f, 4200f))
            {
                continue;
            }

            obstacles.Add(obstacle);
        }

        return obstacles.ToArray();
    }

    private static RockObstacle CreateRandomWalkingRock(Random random, float worldLocalX)
    {
        return random.Next(0, 3) switch
        {
            0 => new RockObstacle(Art.Rock1, worldLocalX, new Point(127, 73)),
            1 => new RockObstacle(Art.Rock2, worldLocalX, new Point(250, 100)),
            _ => new RockObstacle(Art.Rock3, worldLocalX, new Point(250, 100))
        };
    }

    private static bool RangesOverlap(float leftA, float rightA, float leftB, float rightB)
    {
        return leftA < rightB && rightA > leftB;
    }

    private static Rectangle GetIntroVehicleBarrierBounds()
    {
        return new Rectangle(
            (int)MathF.Round(IntroVehiclePosition.X + 36f),
            (int)MathF.Round(IntroVehiclePosition.Y + 330f),
            36,
            210);
    }

    private static Rectangle GetCapsuleLowerBounds()
    {
        return new Rectangle(
            (int)MathF.Round(FinalCapsulePosition.X),
            (int)MathF.Round(FinalCapsulePosition.Y),
            WorldConfig.ReturnCapsuleSize.X,
            WorldConfig.ReturnCapsuleSize.Y);
    }

    private Rectangle GetCapsuleUpperBounds()
    {
        Rectangle lower = GetCapsuleLowerBounds();
        return new Rectangle(
            lower.X,
            (int)MathF.Round(lower.Y + _capsuleUpperOffsetY),
            lower.Width,
            lower.Height);
    }

    private static Rectangle GetCapsuleLowerCollisionBounds()
    {
        Rectangle lower = GetCapsuleLowerBounds();
        Rectangle local = WorldConfig.ReturnCapsuleLowerCollisionLocalRect;
        return new Rectangle(lower.X + local.X, lower.Y + local.Y, local.Width, local.Height);
    }

    private static Vector2 GetFinalPlayerStartPosition(LevelToFinalWalkTransition transition)
    {
        if (transition == null)
        {
            return FinalPlayerStart;
        }

        return FinalVehiclePosition + transition.PlayerVehicleLocalPosition;
    }

    private static IEnumerable<Rectangle> GetFinalVehiclePlatformWorldRectangles()
    {
        foreach (Rectangle localRect in FinalVehiclePlatformLocalRectangles)
        {
            yield return new Rectangle(
                (int)MathF.Round(FinalVehiclePosition.X) + localRect.X,
                (int)MathF.Round(FinalVehiclePosition.Y) + localRect.Y,
                localRect.Width,
                localRect.Height);
        }
    }

    private Rectangle GetCapsuleEntranceBounds()
    {
        Rectangle upper = GetCapsuleUpperBounds();
        Rectangle local = WorldConfig.ReturnCapsuleEntranceLocalRect;
        return new Rectangle(upper.X + local.X, upper.Y + local.Y, local.Width, local.Height);
    }

    private void DrawBackground(SpriteBatch spriteBatch)
    {
        BackgroundRenderer.DrawLooping(spriteBatch, 0f, _worldWidth);
    }

    private void DrawGround(SpriteBatch spriteBatch)
    {
        _ = spriteBatch;
    }

    private static void DrawIntroVehicleModules(SpriteBatch spriteBatch)
    {
        DrawIntroVehicleTexture(spriteBatch, Art.FuelPortClosed, WorldConfig.FuelPortTopLeftLocal, WorldConfig.FuelPortSize, Color.White);
        DrawIntroVehicleTexture(spriteBatch, Art.FuelButtonIdle, WorldConfig.FuelButtonTopLeftLocal, WorldConfig.FuelButtonSize, Color.White);
        DrawIntroVehicleTexture(spriteBatch, Art.FuelButtonIdle, WorldConfig.HandbrakeButtonTopLeftLocal, WorldConfig.FuelButtonSize, Color.White);
        DrawIntroVehicleTexture(spriteBatch, Art.Throttle, WorldConfig.ThrottleIdleTopLeftLocal, WorldConfig.ThrottleSize, Color.White);
        DrawIntroVehicleTexture(spriteBatch, Art.AutoPickupModule, WorldConfig.AutoPickupModuleTopLeftLocal, WorldConfig.AutoPickupModuleSize, Color.Red);
        DrawIntroFuelDisplay(spriteBatch);
    }

    private void DrawFinalVehicleModules(SpriteBatch spriteBatch)
    {
        if (_finalTransition == null)
        {
            return;
        }

        Art fuelPortArt = _finalTransition.FuelPortLit ? Art.FuelPortLit : Art.FuelPortClosed;
        DrawFinalVehicleTexture(spriteBatch, fuelPortArt, WorldConfig.FuelPortTopLeftLocal, WorldConfig.FuelPortSize, GetDamageTint(_finalTransition.FuelPortDamaged));
        DrawFinalVehicleTexture(spriteBatch, Art.FuelButtonIdle, WorldConfig.FuelButtonTopLeftLocal, WorldConfig.FuelButtonSize, GetDamageTint(_finalTransition.FuelPortDamaged));
        DrawFinalVehicleTexture(spriteBatch, _finalTransition.HandbrakeActive ? Art.FuelButtonPressed : Art.FuelButtonIdle, WorldConfig.HandbrakeButtonTopLeftLocal, WorldConfig.FuelButtonSize, Color.White);
        DrawFinalVehicleTexture(spriteBatch, Art.Throttle, WorldConfig.ThrottleIdleTopLeftLocal, WorldConfig.ThrottleSize, GetDamageTint(_finalTransition.ThrottleDamaged));
        DrawFinalVehicleTexture(spriteBatch, Art.AutoPickupModule, WorldConfig.AutoPickupModuleTopLeftLocal, WorldConfig.AutoPickupModuleSize, GetDamageTint(_finalTransition.AutoPickupDamaged));

        if (_finalTransition.SolarPanelInstalled)
        {
            Art solarPanelArt = Game1.SolarPanelHasEnergy && !_finalTransition.SolarPanelDamaged ? Art.SolarPanelPowered : Art.SolarPanelUnpowered;
            DrawFinalVehicleTexture(spriteBatch, solarPanelArt, WorldConfig.SolarPanelTopLeftLocal, WorldConfig.SolarPanelSize, GetDamageTint(_finalTransition.SolarPanelDamaged));
            Art solarButtonArt = _finalTransition.SolarDriveActive && Game1.SolarPanelHasEnergy && !_finalTransition.SolarPanelDamaged ? Art.FuelButtonPressed : Art.FuelButtonIdle;
            DrawFinalVehicleTexture(spriteBatch, solarButtonArt, WorldConfig.SolarButtonTopLeftLocal, WorldConfig.FuelButtonSize, GetDamageTint(_finalTransition.SolarPanelDamaged));
        }

        DrawFinalFuelDisplay(spriteBatch);
    }

    private static void DrawIntroVehicleTexture(SpriteBatch spriteBatch, Art art, Vector2 localPosition, Point size, Color tint)
    {
        spriteBatch.Draw(AssetManager.GetTexture(art), GetIntroVehicleModuleBounds(localPosition, size), tint);
    }

    private static void DrawFinalVehicleTexture(SpriteBatch spriteBatch, Art art, Vector2 localPosition, Point size, Color tint)
    {
        spriteBatch.Draw(AssetManager.GetTexture(art), GetFinalVehicleModuleBounds(localPosition, size), tint);
    }

    private static void DrawIntroFuelDisplay(SpriteBatch spriteBatch)
    {
        Vector2 localPosition = new(
            WorldConfig.FuelDisplayBottomLeftLocal.X,
            WorldConfig.FuelDisplayBottomLeftLocal.Y - WorldConfig.FuelDisplaySize.Y);
        Rectangle bounds = GetIntroVehicleModuleBounds(localPosition, WorldConfig.FuelDisplaySize);
        int filledHeight = (int)MathF.Round(bounds.Height * (VehicleResources.InitialFuel / VehicleResources.MaxFuel));
        Rectangle filledRect = new(
            bounds.X,
            bounds.Bottom - filledHeight,
            bounds.Width,
            filledHeight);
        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), filledRect, Color.Blue);
    }

    private void DrawFinalFuelDisplay(SpriteBatch spriteBatch)
    {
        Vector2 localPosition = new(
            WorldConfig.FuelDisplayBottomLeftLocal.X,
            WorldConfig.FuelDisplayBottomLeftLocal.Y - WorldConfig.FuelDisplaySize.Y);
        Rectangle bounds = GetFinalVehicleModuleBounds(localPosition, WorldConfig.FuelDisplaySize);
        int filledHeight = (int)MathF.Round(bounds.Height * MathHelper.Clamp(_finalTransition.FuelRatio, 0f, 1f));
        if (filledHeight <= 0)
        {
            return;
        }

        Rectangle filledRect = new(
            bounds.X,
            bounds.Bottom - filledHeight,
            bounds.Width,
            filledHeight);
        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel), filledRect, Color.Blue);
    }

    private static Rectangle GetIntroVehicleModuleBounds(Vector2 localPosition, Point size)
    {
        return new Rectangle(
            (int)MathF.Round(IntroVehiclePosition.X + localPosition.X),
            (int)MathF.Round(IntroVehiclePosition.Y + localPosition.Y),
            size.X,
            size.Y);
    }

    private static Rectangle GetFinalVehicleModuleBounds(Vector2 localPosition, Point size)
    {
        return new Rectangle(
            (int)MathF.Round(FinalVehiclePosition.X + localPosition.X),
            (int)MathF.Round(FinalVehiclePosition.Y + localPosition.Y),
            size.X,
            size.Y);
    }

    private Color GetDamageTint(bool isDamaged)
    {
        if (!isDamaged)
        {
            return Color.White;
        }

        int frame = (int)(_moduleDamageBlinkTimer / DamageBlinkInterval);
        return frame % 2 == 0 ? Color.Red : Color.White;
    }

    private void DrawCapsule(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(AssetManager.GetTexture(Art.ApolloUpperHalf), GetCapsuleUpperBounds(), Color.White);
        spriteBatch.Draw(AssetManager.GetTexture(Art.ApolloLowerHalf), GetCapsuleLowerBounds(), Color.White);
    }

    private void DrawStormOverlay(SpriteBatch spriteBatch)
    {
        float alpha = GetStormAlpha();
        if (alpha <= 0f)
        {
            return;
        }

        Texture2D sandstorm = AssetManager.GetTexture(Art.Sandstorm);
        float startX = WorldConfig.ScreenWidth - (_stormOffset % StormSheetScreenWidth);
        for (int i = -2; i <= 1; i++)
        {
            Rectangle bounds = new(
                (int)MathF.Round(startX + i * StormSheetScreenWidth),
                0,
                (int)StormSheetScreenWidth,
                WorldConfig.ScreenHeight);
            spriteBatch.Draw(sandstorm, bounds, Color.White * alpha);
        }
    }

    private void DrawHud(SpriteBatch spriteBatch)
    {
        if (!Game1.Debug)
        {
            return;
        }

        Vector2 startPosition = _mode == WalkingSceneMode.Intro ? IntroPlayerStart : FinalPlayerStart;
        float walkedDistance = MathF.Max(0f, _player.Position.X - startPosition.X);
        string sceneName = _mode == WalkingSceneMode.Intro ? "DEBUG  INTRO WALK" : "DEBUG  FINAL WALK";
        List<HudLine> lines = new()
        {
            new HudLine("Distance  " + walkedDistance.ToString("0.0") + "    Player X  " + _player.Position.X.ToString("0.0"), Color.LightGreen)
        };

        if (_stormTimer > 0f)
        {
            lines.Add(new HudLine("Storm interference  " + MathF.Ceiling(_stormTimer).ToString("0") + "s", Color.Yellow));
        }

        DrawSimpleDebugText(spriteBatch, sceneName, lines);
    }

    private void DrawPauseOverlay(SpriteBatch spriteBatch)
    {
        if (!_isPaused)
        {
            return;
        }

        string objective = _mode == WalkingSceneMode.Intro
            ? "Reach the rover beacon"
            : "Reach the return capsule";

        DrawPausePage(spriteBatch, "Esc  Resume", new List<HudLine>
        {
            new HudLine("Esc                    Pause / Resume", new Color(224, 244, 248)),
            new HudLine("WASD / Arrow Keys       Move", Color.White),
            new HudLine("Space / W / Up          Jump", Color.White),
            new HudLine("G / Enter / Space       Advance dialogue", Color.White),
            new HudLine("Objective               " + objective, new Color(213, 232, 235)),
            new HudLine("F3                      Toggle debug overlay", new Color(255, 232, 150))
        });
    }

    private float GetStormAlpha()
    {
        if (_stormTimer <= 0f)
        {
            return 0f;
        }

        float baseAlpha = _mode == WalkingSceneMode.Intro ? 0.9f : 0.82f;
        float fade = MathHelper.Clamp(_stormTimer / 2.25f, 0f, 1f);
        return baseAlpha * fade;
    }

    private static List<WalkingDialogueDefinition> LoadDialogueDefinitions(WalkingSceneMode mode)
    {
        string fileName = mode == WalkingSceneMode.Intro
            ? "intro_walk_dialogues.json"
            : "final_walk_dialogues.json";
        string path = GetDataFilePath(fileName);
        if (!File.Exists(path))
        {
            return new List<WalkingDialogueDefinition>();
        }

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<WalkingDialogueDefinition>>(json, JsonOptions) ?? new List<WalkingDialogueDefinition>();
    }

    private static string GetDataFilePath(string fileName)
    {
        DirectoryInfo directory = new(Environment.CurrentDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ProjectFileName)))
            {
                return Path.Combine(directory.FullName, "data", fileName);
            }

            directory = directory.Parent;
        }

        return Path.Combine(Environment.CurrentDirectory, "data", fileName);
    }
}

public class WalkingDialogueDefinition
{
    public string Id { get; set; }
    public string Trigger { get; set; }
    public float TriggerDistance { get; set; }
    public List<DialogueLine> Dialogue { get; set; }
}
