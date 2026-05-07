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
    private const int IntroWorldWidth = 3200;
    private const int FinalWorldWidth = 3000;
    private const int GroundHeight = 160;

    private static readonly Vector2 IntroPlayerStart = new(160f, WorldConfig.FakeGroundLocalRect.Y - 48f);
    private static readonly Vector2 IntroVehiclePosition = new(1900f, WorldConfig.FakeGroundLocalRect.Y - 540f);
    private static readonly Vector2 FinalPlayerStart = new(160f, WorldConfig.FakeGroundLocalRect.Y - 48f);
    private static readonly Vector2 FinalCapsulePosition = new(1850f, WorldConfig.FakeGroundLocalRect.Y - 256f);

    private readonly WalkingSceneMode _mode;
    private readonly TextBoxEntity _textBox = new();
    private readonly Player _player;
    private readonly VehicleEntity _introVehicle;
    private readonly List<WalkingDialogueDefinition> _dialogueDefinitions;
    private readonly HashSet<string> _triggeredDialogueIds = new();
    private readonly int _worldWidth;
    private readonly Rectangle _movementBounds;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private float _cameraX;
    private float _stormTimer;
    private float _stormOffset;
    private string _pendingScene;
    private bool _isLaunching;
    private float _capsuleUpperOffsetY;

    public WalkingScene(WalkingSceneMode mode)
    {
        _mode = mode;
        _worldWidth = mode == WalkingSceneMode.Intro ? IntroWorldWidth : FinalWorldWidth;
        _movementBounds = new Rectangle(0, 0, _worldWidth, WorldConfig.WorldHeight);
        _stormTimer = mode == WalkingSceneMode.Intro ? IntroStormDuration : FinalStormDuration;
        _dialogueDefinitions = LoadDialogueDefinitions(mode);

        if (mode == WalkingSceneMode.Intro)
        {
            Game1.SolarPanelEnabled = false;
            Game1.ResetSolarPanelEnergyState();
            _player = new Player(IntroPlayerStart);
            _introVehicle = new VehicleEntity();
            _introVehicle.SetPosition(IntroVehiclePosition);
            _introVehicle.SetScene(this);
        }
        else
        {
            _player = new Player(FinalPlayerStart);
            StartTriggeredDialogue(SceneStartTrigger);
        }

        _player.SetScene(this);
        UpdateCamera(1f / 60f);
    }

    public bool IsPlayerCarryingItem => false;
    public bool IsPlayerInputLocked => _textBox.IsActiveDialogue || _isLaunching;
    public bool CanPlayerMoveLeft => _mode == WalkingSceneMode.Final;
    public float VehicleSpeed => 0f;
    public Rectangle PlayerMovementBounds => _movementBounds;

    public bool IsPlayerAttachedToVehicle(Player player)
    {
        return false;
    }

    public IEnumerable<Rectangle> GetSolidRectangles()
    {
        yield return GetGroundBounds();

        if (_mode == WalkingSceneMode.Intro)
        {
            yield return GetIntroVehicleBarrierBounds();
        }

        if (_mode == WalkingSceneMode.Final)
        {
            yield return GetCapsuleLowerCollisionBounds();
        }
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        UpdateStorm(dt);

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
        }
        else
        {
            DrawCapsule(spriteBatch);
        }

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
        _textBox.Draw(spriteBatch);
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
            yield return GetCapsuleLowerCollisionBounds();
            yield return GetCapsuleEntranceBounds();
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

    private void UpdateCamera(float dt)
    {
        float targetX = MathHelper.Clamp(
            _player.Position.X - WorldConfig.ScreenWidth * CameraFollowAnchorX,
            0f,
            MathF.Max(0f, _worldWidth - WorldConfig.ScreenWidth));

        float smoothing = dt <= 0f ? 1f : 1f - MathF.Exp(-CameraSharpness * dt);
        _cameraX = MathHelper.Lerp(_cameraX, targetX, smoothing);
    }

    private Matrix GetViewMatrix()
    {
        return Matrix.CreateTranslation(-_cameraX, -GetCameraY(), 0f);
    }

    private static float GetCameraY()
    {
        return WorldConfig.FakeGroundLocalRect.Y - WorldConfig.ScreenHeight + 120f;
    }

    private Rectangle GetGroundBounds()
    {
        return new Rectangle(0, WorldConfig.FakeGroundLocalRect.Y, _worldWidth, GroundHeight);
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

    private Rectangle GetCapsuleEntranceBounds()
    {
        Rectangle upper = GetCapsuleUpperBounds();
        Rectangle local = WorldConfig.ReturnCapsuleEntranceLocalRect;
        return new Rectangle(upper.X + local.X, upper.Y + local.Y, local.Width, local.Height);
    }

    private void DrawBackground(SpriteBatch spriteBatch)
    {
        Texture2D background = AssetManager.GetTexture(Art.Background);
        for (int x = 0; x < _worldWidth + background.Width; x += background.Width)
        {
            spriteBatch.Draw(background, new Rectangle(x, 0, background.Width, WorldConfig.WorldHeight), Color.White);
        }
    }

    private static void DrawGround(SpriteBatch spriteBatch)
    {
        _ = spriteBatch;
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
