using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class Scene
{
    private List<Entity> _entities = new List<Entity>();
    private Grid _grid;

    public Grid Grid => _grid;
    public bool finished = false;
    public string nextScene = "";

    public Scene()
    {
        _grid = new Grid();
    }

    public Scene(string levelName)
    {
        LoadLevel(levelName);
    }

    private void LoadLevel(string levelName)
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "data", levelName + ".csv");

        if (!File.Exists(filePath))
        {
            string projectRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            string projectDataPath = Path.Combine(projectRootPath, "data", levelName + ".csv");
            if (File.Exists(projectDataPath))
            {
                filePath = projectDataPath;
            }
        }

        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine(filePath + " not found");
            _grid = new Grid();
            return;
        }

        string content = File.ReadAllText(filePath);
        ParseLevelText(content);
    }

    void ParseLevelText(string content)
    {
        _grid = new Grid();
        string[] lines = content.Split('\n');
        for (int y = 0; y < lines.Length; y++)
        {
            string[] line = lines[y].Split(',');
            for (int x = 0; x < line.Length; x++)
            {
                Point p = new Point(x, y);
                string textData = line[x].Trim();
                if (textData == string.Empty)
                    continue;
                bool entitySpawned = CheckForSpawnEntity(p, textData);

                char tileSymbol = entitySpawned ? '\0' : textData[0];
                if (Tile.tileSymbols.ContainsKey(tileSymbol))
                {
                    _grid.SetTile(p, Tile.tileSymbols[tileSymbol]);
                }
            }
        }
    }

    bool CheckForSpawnEntity(Point gridPosition, string symbol)
    {
        Vector2 pixelPosition =
            Grid.GetPixelPositionFromGridPosition(gridPosition);

        switch (symbol)
        {
            case "player":
                AddEntity(new Player(Art.Player, pixelPosition));
                return true;
            case "enemy":
                AddEntity(new EnemyEntity(pixelPosition));
                return true;
            case "enemy2":
                AddEntity(new Enemy2Entity(pixelPosition));
                return true;
            case "A":
                AddEntity(new Enemy2Entity(pixelPosition));
                return true;
            case "^":
                AddEntity(new SpikeEntity(pixelPosition));
                return true;
        }

        return false;
    }

    public void AddEntity(Entity entity)
    {
        _entities.Add(entity);
        entity.SetScene(this);
    }

    public T FindFirstEntity<T>() where T : Entity
    {
        foreach (Entity entity in _entities)
        {
            if (entity is T t)
            {
                return t;
            }
        }
        return null;
    }

    public IEnumerable<T> FindEntities<T>() where T : Entity
    {
        foreach (Entity entity in _entities)
        {
            if (entity is T t)
            {
                yield return t;
            }
        }
    }

    public void RemoveEntity(Entity entity)
    {
        _entities.Remove(entity);
        entity.RemoveFromScene(this);
        entity.SetScene(null);
    }

    public virtual void Update(GameTime gameTime)
    {
        for (int i = _entities.Count - 1; i >= 0; i--)
        {
            Entity entity = _entities[i];
            if (!entity.IsActive)
            {
                _entities.RemoveAt(i);
                entity.RemoveFromScene(this);
                entity.SetScene(null);
                continue;
            }

            entity.Update(gameTime);
        }
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        _grid.draw(spriteBatch);
        foreach (Entity entity in _entities)
        {
            entity.Draw(spriteBatch);
        }
    }

    public virtual void Open()
    {

    }

    public virtual void Close()
    {
        finished = false;
        nextScene = "";
    }
    
    public virtual void RestartLevel()
    {
    }

    public void ChangeScene(string _nextScene)
    {
        finished = true;
        nextScene = _nextScene;
    }

    public bool DoesEntityOverlapGrid(Entity entity, Vector2 velocity)
    {
        Rectangle baseBounds = entity.GetBounds();
        Rectangle nextRect = new(
            baseBounds.X + (int)velocity.X,
            baseBounds.Y + (int)velocity.Y,
            baseBounds.Width,
            baseBounds.Height
        );

        int left = nextRect.Left;
        int right = Math.Max(nextRect.Left, nextRect.Right - 1);
        int top = nextRect.Top;
        int bottom = Math.Max(nextRect.Top, nextRect.Bottom - 1);

        bool topRightSolid = _grid.IsTileSolid(
            new Vector2(right, top)
        );
        bool botRightSolid = _grid.IsTileSolid(
            new Vector2(right, bottom)
        );
        bool botLeftSolid = _grid.IsTileSolid(
            new Vector2(left, bottom)
        );
        bool topLeftSolid = _grid.IsTileSolid(
            new Vector2(left, top)
        );
        bool center = _grid.IsTileSolid(
            new Vector2(nextRect.Center.X, nextRect.Center.Y)
        );

        return topRightSolid
            || botRightSolid
            || botLeftSolid
            || topLeftSolid
            || center;
    }

    public Vector2 CheckForGridCollision(Entity entity, Vector2 velocity)
    {
        Rectangle bounds = entity.GetBounds();
        if (bounds == Rectangle.Empty || _grid == null)
        {
            return velocity;
        }

        bool IsSolidAt(int pixelX, int pixelY)
        {
            return _grid.IsTileSolid(new Vector2(pixelX, pixelY));
        }

        bool IsXMoveBlocked(Rectangle rect, int dirX)
        {
            int sampleX = dirX > 0 ? rect.Right - 1 : rect.Left;
            int top = rect.Top + 1;
            int bottom = Math.Max(top, rect.Bottom - 2);
            int mid = (top + bottom) / 2;

            return IsSolidAt(sampleX, top)
                   || IsSolidAt(sampleX, mid)
                   || IsSolidAt(sampleX, bottom);
        }

        bool IsYMoveBlocked(Rectangle rect, int dirY)
        {
            int sampleY = dirY > 0 ? rect.Bottom - 1 : rect.Top;
            int left = rect.Left + 1;
            int right = Math.Max(left, rect.Right - 2);
            int mid = (left + right) / 2;

            return IsSolidAt(left, sampleY)
                   || IsSolidAt(mid, sampleY)
                   || IsSolidAt(right, sampleY);
        }

        float allowedX = 0f;
        int dirX = Math.Sign(velocity.X);
        int wholeX = (int)Math.Floor(Math.Abs(velocity.X));
        for (int i = 0; i < wholeX && dirX != 0; i++)
        {
            Rectangle testRect = new Rectangle(bounds.X + dirX, bounds.Y, bounds.Width, bounds.Height);
            if (IsXMoveBlocked(testRect, dirX))
            {
                break;
            }

            bounds = testRect;
            allowedX += dirX;
        }

        float remainingX = velocity.X - allowedX;
        if (Math.Abs(remainingX) > 0f)
        {
            Rectangle testRect = new Rectangle(bounds.X + (int)remainingX, bounds.Y, bounds.Width, bounds.Height);
            if (!IsXMoveBlocked(testRect, Math.Sign(remainingX)))
            {
                bounds = testRect;
                allowedX += remainingX;
            }
        }

        float allowedY = 0f;
        int dirY = Math.Sign(velocity.Y);
        int wholeY = (int)Math.Floor(Math.Abs(velocity.Y));
        for (int i = 0; i < wholeY && dirY != 0; i++)
        {
            Rectangle testRect = new Rectangle(bounds.X, bounds.Y + dirY, bounds.Width, bounds.Height);
            if (IsYMoveBlocked(testRect, dirY))
            {
                break;
            }

            bounds = testRect;
            allowedY += dirY;
        }

        float remainingY = velocity.Y - allowedY;
        if (Math.Abs(remainingY) > 0f)
        {
            Rectangle testRect = new Rectangle(bounds.X, bounds.Y + (int)remainingY, bounds.Width, bounds.Height);
            if (!IsYMoveBlocked(testRect, Math.Sign(remainingY)))
            {
                allowedY += remainingY;
            }
        }

        return new Vector2(allowedX, allowedY);
    }
}
