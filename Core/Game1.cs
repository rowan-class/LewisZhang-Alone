using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LewisZhang_Alone;

public enum DebugStartLevel
{
    None,
    IntroWalk,
    Vehicle,
    FinalWalk,
    GameOver,
    Complete
}

public class Game1 : Game
{
    private const float BackgroundCrossfadeDuration = 2.4f;

    // Default debug state. Press F3 in-game to toggle collision bounds.
    public static bool Debug = true;
    // Set this to jump straight into a level while debugging.
    public static DebugStartLevel DebugStartLevel = DebugStartLevel.Vehicle;
// DebugStartLevel.None       // 正常从 start screen 开始
// DebugStartLevel.IntroWalk  // 直接进开头走路场景
// DebugStartLevel.Vehicle    // 直接进车 level
// DebugStartLevel.FinalWalk  // 直接进结尾走路场景
// DebugStartLevel.GameOver   // 直接进 game over 页面
// DebugStartLevel.Complete   // 直接进 mission complete 页面
    // Flip this to show / enable the solar module.
    public static bool SolarPanelEnabled = false;
    public static bool SolarPanelHasEnergy = true;
    public static bool SolarPanelIsDaytime = true;
    public static bool SolarPanelBlockedByWeather = false;
    private static float _backgroundNightBlend;
    private static float _backgroundFadeStartBlend;
    private static float _backgroundFadeTargetBlend;
    private static float _backgroundFadeTimer;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _entityBatch;

    public static Vector2 ScreenSize = WorldConfig.ScreenSize;
    public static float BackgroundNightBlend => _backgroundNightBlend;

    private Dictionary<string, Func<Scene>> _sceneFactories;
    private Scene _currentScene;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = (int)ScreenSize.X;
        _graphics.PreferredBackBufferHeight = (int)ScreenSize.Y;
    }

    public static void ResetSolarPanelEnergyState()
    {
        SolarPanelIsDaytime = true;
        SolarPanelBlockedByWeather = false;
        SetBackgroundDaytimeImmediate(true);
        RefreshSolarPanelEnergy();
    }

    public static void SetSolarPanelDaytime(bool isDaytime)
    {
        if (SolarPanelIsDaytime != isDaytime)
        {
            BeginBackgroundCrossfade(isDaytime);
        }

        SolarPanelIsDaytime = isDaytime;
        RefreshSolarPanelEnergy();
    }

    public static void SetSolarPanelWeatherBlocked(bool isBlocked)
    {
        SolarPanelBlockedByWeather = isBlocked;
        RefreshSolarPanelEnergy();
    }

    public static void RestoreSolarPanelEnergyState(bool isDaytime, bool isBlockedByWeather)
    {
        SolarPanelIsDaytime = isDaytime;
        SolarPanelBlockedByWeather = isBlockedByWeather;
        SetBackgroundDaytimeImmediate(isDaytime);
        RefreshSolarPanelEnergy();
    }

    private static void RefreshSolarPanelEnergy()
    {
        SolarPanelHasEnergy = SolarPanelIsDaytime && !SolarPanelBlockedByWeather;
    }

    private static void BeginBackgroundCrossfade(bool isDaytime)
    {
        _backgroundFadeStartBlend = _backgroundNightBlend;
        _backgroundFadeTargetBlend = isDaytime ? 0f : 1f;
        _backgroundFadeTimer = BackgroundCrossfadeDuration;
    }

    private static void SetBackgroundDaytimeImmediate(bool isDaytime)
    {
        _backgroundNightBlend = isDaytime ? 0f : 1f;
        _backgroundFadeStartBlend = _backgroundNightBlend;
        _backgroundFadeTargetBlend = _backgroundNightBlend;
        _backgroundFadeTimer = 0f;
    }

    private static void UpdateBackgroundCrossfade(GameTime gameTime)
    {
        if (_backgroundFadeTimer <= 0f)
        {
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _backgroundFadeTimer = MathF.Max(0f, _backgroundFadeTimer - dt);
        float progress = 1f - MathHelper.Clamp(_backgroundFadeTimer / BackgroundCrossfadeDuration, 0f, 1f);
        float smoothProgress = MathHelper.SmoothStep(0f, 1f, progress);
        _backgroundNightBlend = MathHelper.Lerp(_backgroundFadeStartBlend, _backgroundFadeTargetBlend, smoothProgress);

        if (_backgroundFadeTimer <= 0f)
        {
            _backgroundNightBlend = _backgroundFadeTargetBlend;
        }
    }

    protected override void Initialize()
    {
        ServiceLocator.Game1 = this;
        ServiceLocator.Input = new Input();
        AssetManager.LoadContent(Content, GraphicsDevice);

        _sceneFactories = new Dictionary<string, Func<Scene>>
        {
            ["start"] = () => new StartScene(),
            ["newGame"] = () =>
            {
                SaveManager.DeleteSave();
                return new WalkingScene(WalkingSceneMode.Intro);
            },
            ["continue"] = () => new LevelScene(SaveManager.Load()),
            ["level1"] = () => new LevelScene(),
            ["finalWalk"] = () => new WalkingScene(WalkingSceneMode.Final),
            ["end"] = () => new EndScene(),
            ["gameOver"] = () => new GameOverScene(),
        };

        _currentScene = _sceneFactories[GetInitialSceneKey()]();
        _currentScene.Open();

        base.Initialize();
    }

    private static string GetInitialSceneKey()
    {
        return DebugStartLevel switch
        {
            DebugStartLevel.IntroWalk => "newGame",
            DebugStartLevel.Vehicle => "level1",
            DebugStartLevel.FinalWalk => "finalWalk",
            DebugStartLevel.GameOver => "gameOver",
            DebugStartLevel.Complete => "end",
            _ => "start"
        };
    }

    protected override void LoadContent()
    {
        _entityBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        ServiceLocator.Input.Update();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || ServiceLocator.Input.IsActionDown(Action.ExitGame))
        {
            Exit();
        }

        if (ServiceLocator.Input.IsActionPressed(Action.ToggleDebug))
        {
            Debug = !Debug;
        }

        UpdateBackgroundCrossfade(gameTime);
        _currentScene.Update(gameTime);

        if (_currentScene.finished)
        {
            string nextScene = _currentScene.nextScene;
            _currentScene.Close();
            if (!string.IsNullOrEmpty(nextScene) && _sceneFactories.TryGetValue(nextScene, out Func<Scene> factory))
            {
                _currentScene = factory();
                _currentScene.Open();
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _currentScene.Draw(_entityBatch);
        base.Draw(gameTime);
    }
}
