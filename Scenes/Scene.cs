using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class Scene
{
    private readonly List<Entity> _entities = new();

    public bool finished = false;
    public string nextScene = "";

    public virtual void Open()
    {
    }

    public virtual void Close()
    {
        finished = false;
        nextScene = "";
    }

    public void AddEntity(Entity entity)
    {
        _entities.Add(entity);
        entity.SetScene(this);
    }

    public void RemoveEntity(Entity entity)
    {
        _entities.Remove(entity);
        entity.RemoveFromScene(this);
        entity.SetScene(null);
    }

    public T FindFirstEntity<T>() where T : Entity
    {
        foreach (Entity entity in _entities)
        {
            if (entity is T typedEntity)
            {
                return typedEntity;
            }
        }

        return null;
    }

    public IEnumerable<T> FindEntities<T>() where T : Entity
    {
        foreach (Entity entity in _entities)
        {
            if (entity is T typedEntity)
            {
                yield return typedEntity;
            }
        }
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
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawEntities(spriteBatch, _entities);
        if (Game1.Debug)
        {
            DrawDebugRectangles(spriteBatch, GetDebugRectangles());
        }
        spriteBatch.End();
    }

    public void ChangeScene(string nextSceneKey)
    {
        finished = true;
        nextScene = nextSceneKey;
    }

    protected void DrawEntities(SpriteBatch spriteBatch, IEnumerable<Entity> entities)
    {
        foreach (Entity entity in entities)
        {
            entity.Draw(spriteBatch);
        }
    }

    protected virtual IEnumerable<Rectangle> GetDebugRectangles()
    {
        foreach (Entity entity in _entities)
        {
            foreach (Rectangle rect in entity.GetDebugRectangles())
            {
                yield return rect;
            }
        }
    }

    protected void DrawDebugRectangles(SpriteBatch spriteBatch, IEnumerable<Rectangle> rectangles)
    {
        foreach (Rectangle rect in rectangles)
        {
            DrawDebugRectangle(spriteBatch, rect, Color.Red);
        }
    }

    protected void DrawDebugRectangle(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        if (bounds == Rectangle.Empty || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        Texture2D pixel = AssetManager.GetTexture(Art.pixel);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Bottom - 1, bounds.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, 1, bounds.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - 1, bounds.Top, 1, bounds.Height), color);
    }
}
