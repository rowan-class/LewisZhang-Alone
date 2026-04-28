using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Alone;

public class Grid
{
    public const int TileSize = 64;
    private Dictionary<Point, Tile> tiles;

    public Grid()
    {
        tiles = new Dictionary<Point, Tile>();
    }

    public void SetTile(Point pos, TileType type)
    {
        Tile tile = new Tile(pos, type);
        tiles[pos] = tile;
    }

    public void RemoveTile(Point position)
    {
        tiles.Remove(position);
    }

    private bool HasPositionWithinGridSize(Point gridPosition)
    {
        return tiles.ContainsKey(gridPosition);
    }

    public Tile GetTile(Point gridPosition)
    {
        if (HasPositionWithinGridSize(gridPosition) == false) return null;
        return tiles[gridPosition];
    }

    public Tile GetTile(Vector2 pixelPosition)
    {
        return GetTile(GetGridPositionFromPixelPosition(pixelPosition));
    }

    public bool IsTileSolid(Point gridPosition)
    {
        Tile tile = GetTile(gridPosition);
        if (tile == null) {return false;}
        return Tile.tileSolid[tile.Type];
    }

    public bool IsTileSolid(Vector2 pixelPosition)
    {
        return IsTileSolid(GetGridPositionFromPixelPosition(pixelPosition));
    }

    public void draw(SpriteBatch spriteBatch)
    {
        foreach (Tile tile in tiles.Values)
        {
            tile.Draw(spriteBatch);
        }
    }

    // Legacy misspelling kept to avoid breaking existing calls.
    public static Vector2 GetPixelPositionFronGridPosition(Point gridPos)
    {
        return new Vector2(gridPos.X * TileSize, gridPos.Y * TileSize);
    }

    public static Vector2 GetPixelPositionFromGridPosition(Point gridPos)
    {
        return GetPixelPositionFronGridPosition(gridPos);
    }

    public static Point GetGridPositionFromPixelPosition(Vector2 pixelPos)
    {
        Point gridPos = new Point();
        gridPos.X = (int)Math.Floor(pixelPos.X / TileSize);
        gridPos.Y = (int)Math.Floor(pixelPos.Y / TileSize);
        return gridPos;
    }

}

public enum TileType
{
    Empty,
    Wall,
    Floor,
    Exit
}

public class Tile
{
    static Dictionary<TileType, Color> tileColors = new Dictionary<TileType, Color>()
    {
        {TileType.Empty, new Color(8, 9, 0, 0)},
        {TileType.Floor, Color.SaddleBrown},
        {TileType.Wall, Color.Black},
        {TileType.Exit, Color.Yellow}
    };

    public static Dictionary<TileType, bool> tileSolid = new Dictionary<TileType, bool>()
    {
        {TileType.Empty, false},
        {TileType.Floor, false},
        {TileType.Wall, true},
        {TileType.Exit, false}
    };

    public static Dictionary<char, TileType> tileSymbols = new Dictionary<char, TileType>()
    {
        {'_', TileType.Floor},
        {'#', TileType.Wall},
        {'X', TileType.Exit}
    };

    protected Point _gridPosition;
    protected TileType _tileType;

    public Point GridPosition => _gridPosition;
    public Point PixelPosition => new Point(_gridPosition.X * Grid.TileSize, _gridPosition.Y * Grid.TileSize);
    public TileType Type => _tileType;

    public Tile(Point gridPosition, TileType tileType)
    {
        _gridPosition = gridPosition;
        _tileType = tileType;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(AssetManager.GetTexture(Art.pixel),
            new Rectangle((int)PixelPosition.X, (int)PixelPosition.Y, Grid.TileSize, Grid.TileSize),
            tileColors[_tileType]);
    }
}
