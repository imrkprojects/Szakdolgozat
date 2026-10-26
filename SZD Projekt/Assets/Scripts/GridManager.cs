using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>Creates and provides access to the match grid and its tiles.</summary>
public class GridManager : MonoBehaviour
{
    /// <summary>The active grid manager instance.</summary>
    public static GridManager Instance { get; private set; }
    
    private const int MinimumDimension = 3;
    
    [Header("Grid Dimensions")]
    [Min(MinimumDimension)]
    [SerializeField] private int width = 20;
    
    [Min(MinimumDimension)]
    [SerializeField] private int height = 10;
    
    [SerializeField] private float tileSize = 1.1f; // 1.0 + slight spacing

    [Header("Prefabs")]
    [SerializeField] private Tile tilePrefab;

    private Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();
    
    /// <summary>Number of columns in the generated grid.</summary>
    public int Width  => width;
    /// <summary>Number of rows in the generated grid.</summary>
    public int Height => height;
    
    /// <summary>Raised after grid generation with the grid's world-space center.</summary>
    public static event Action<Vector3> OnGridGenerated;

    private void OnValidate()
    {
        ClampDimensions();
    }

    private void Awake()
    {
        ClampDimensions();
        
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    
    private void ClampDimensions()
    {
        width = Mathf.Max(MinimumDimension, width);
        height = Mathf.Max(MinimumDimension, height);
    }
    
    /// <summary>Creates the grid tiles and notifies listeners when the grid is ready.</summary>
    public void BuildGrid()
    {
        GenerateGrid();
        Debug.Log($"Grid generated with dimensions {width} x {height}.");
    }

    /// <summary>Returns the tile at the given grid coordinate, or null if it is outside the grid.</summary>
    public Tile GetTile(Vector2Int coords)
    {
        _tiles.TryGetValue(coords, out Tile tile);
        return tile;
    }
    
    private void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPos = new Vector3(x * tileSize, y * tileSize, 0);
                Tile spawnedTile = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                spawnedTile.name = $"Tile_{x}_{y}";
                spawnedTile.Init(x, y);

                _tiles[new Vector2Int(x, y)] = spawnedTile;
            }
        }
        
        OnGridGenerated?.Invoke(GetGridCenter());
    }
    
    /// <summary>Returns the center of the grid in world space.</summary>
    public Vector3 GetGridCenter()
    {
        float centerX = (width * tileSize) / 2f - (tileSize / 2f);
        float centerY = (height * tileSize) / 2f - (tileSize / 2f);
        return new Vector3(centerX, centerY, 0f);
    }
    
    /// <summary>Returns the existing orthogonal neighbors of a tile.</summary>
    public List<Tile> GetAdjacentTiles(Tile tile)
    {
        List<Tile> adjacent = new List<Tile>();
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };
        
        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborCoords = tile.Coords + dir;
            if (_tiles.TryGetValue(neighborCoords, out Tile neighbour))
            {
                adjacent.Add(neighbour);
            }
        }
        return adjacent;
    }
    
    // TODO: Expand functionality supporting dynamic ranges and shapes
    /// <summary>
    /// Returns all tiles within a given Manhattan distance (diamond shape) of the center tile.
    /// </summary>
    /// <param name="center">Origin tile.</param>
    /// <param name="range">Maximum Manhattan distance.</param>
    /// <param name="excludeCenter">If true, the center tile itself is not included.</param>
    public List<Tile> GetTilesInRange(Tile center, int range, bool excludeCenter = true)
    {
        List<Tile> result = new List<Tile>();
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (excludeCenter && dx == 0 && dy == 0) continue;
                // Manhattan distance check — keeps diamond shape, not square
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > range) continue;
                Tile tile = GetTile(center.Coords + new Vector2Int(dx, dy));
                if (tile != null) result.Add(tile);
            }
        }
        return result;
    }

    /// <summary>Forwards a clicked tile to the selection manager.</summary>
    public void OnTileClicked(Tile clickedTile)
    {
        SelectionManager.Instance.HandleTileClicked(clickedTile);
    }
}
