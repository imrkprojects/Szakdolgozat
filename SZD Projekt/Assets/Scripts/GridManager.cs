using UnityEngine;
using System;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }
    
    [Header("Grid Dimensions")]
    [SerializeField] private int width = 20;
    [SerializeField] private int height = 10;
    [SerializeField] private float tileSize = 1.1f; // 1.0 + slight spacing

    [Header("Prefabs")]
    [SerializeField] private Tile tilePrefab;

    private Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();
    
    public int Width  => width;
    public int Height => height;
    
    public static event Action<Vector3> OnGridGenerated;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    
    // Called by MatchManager on init
    public void BuildGrid()
    {
        GenerateGrid();
        Debug.Log($"Grid generated with dimensions {width} x {height}.");
    }

    // Returns the tile at the given grid coordinate, or null if out of bounds
    public Tile GetTile(Vector2Int coords)
    {
        _tiles.TryGetValue(coords, out Tile tile);
        return tile;
    }
    
    private void GenerateGrid()
    {
        for (int x = 0; x < Mathf.Max(3, width); x++)
        {
            for (int y = 0; y < Mathf.Max(3, height); y++)
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
    
    public Vector3 GetGridCenter()
    {
        float centerX = (width * tileSize) / 2f - (tileSize / 2f);
        float centerY = (height * tileSize) / 2f - (tileSize / 2f);
        return new Vector3(centerX, centerY, 0f);
    }
    
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

    public void OnTileClicked(Tile clickedTile)
    {
        SelectionManager.Instance.HandleTileClicked(clickedTile);
    }
}