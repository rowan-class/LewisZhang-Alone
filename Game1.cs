using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LewisZhang_Alone;

public class Game1 : Game
{
    // Default debug state. Press F3 in-game to toggle collision bounds.
    public static bool Debug = true;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _entityBatch;

    public static Vector2 ScreenSize = WorldConfig.ScreenSize;

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

    protected override void Initialize()
    {
        ServiceLocator.Game1 = this;
        ServiceLocator.Input = new Input();
        AssetManager.LoadContent(Content, GraphicsDevice);

        _sceneFactories = new Dictionary<string, Func<Scene>>
        {
            ["start"] = () => new StartScene(),
            ["level1"] = () => new LevelScene(),
        };

        _currentScene = _sceneFactories["start"]();
        _currentScene.Open();

        base.Initialize();
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
