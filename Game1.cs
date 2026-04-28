using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LewisZhang_Alone;

public class Game1 : Game
{
    public static bool Debug = false;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _entityBatch;

    public static Vector2 ScreenSize = new Vector2(1280, 768);

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
        ServiceLocator.GameState = new GameState();
        // setup input service
        ServiceLocator.Input = new Input();
        AssetManager.LoadContent(Content, GraphicsDevice);

        _sceneFactories = new Dictionary<string, Func<Scene>>
        {
            ["start"] = () => new StartScene(),
            ["level_select"] = () => new LevelSelectScene(),
            ["level1"] = () => new LevelScene("level1", "level1", "level2"),
            ["level2"] = () => new LevelScene("level2", "level2", "end"),
            ["end"] = () => new EndScene(),
        };

        _currentScene = _sceneFactories["start"]();
        _currentScene.Open();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _entityBatch = new SpriteBatch(GraphicsDevice);

        // TODO: use this.Content to load your game content here
    }
     
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        ServiceLocator.Input.Update();
        
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

        // TODO: Add your drawing code here
        _entityBatch.Begin();

        _currentScene.Draw(_entityBatch);

        _entityBatch.End();

        base.Draw(gameTime);
    }
}
