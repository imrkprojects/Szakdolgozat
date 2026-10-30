using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>Creates and provides access to the match grid and its tiles.</summary>
public class GridManager : MonoBehaviour
{
    private const int MinimumDimension = 3;

    [Header("Grid Dimensions")] [Min(MinimumDimension)] [SerializeField]
    private int width = 20;

    [Min(MinimumDimension)] [SerializeField]
    private int height = 10;

    [SerializeField] private float tileSize = 1.1f; // 1.0 + slight spacing

    [Header("Prefabs")] [SerializeField] private Tile tilePrefab;

    private Dictionary<Vector2Int, Tile> _tiles = new Dictionary<Vector2Int, Tile>();
    private readonly Dictionary<UnitState, Unit> _unitViews = new Dictionary<UnitState, Unit>();
    private Action<Tile> _onTileClicked;

    /// <summary>Game state of the grid; set by <see cref="BuildGrid"/>.</summary>
    public Board Board { get; private set; }

    /// <summary>Number of columns in the generated grid.</summary>
    public int Width => width;

    /// <summary>Number of rows in the generated grid.</summary>
    public int Height => height;

    private void OnValidate()
    {
        ClampDimensions();
    }

    private void Awake()
    {
        ClampDimensions();
    }

    private void ClampDimensions()
    {
        width = Mathf.Max(MinimumDimension, width);
        height = Mathf.Max(MinimumDimension, height);
    }

    /// <summary>Creates a tile for every board cell.</summary>
    /// <param name="board">Game state the tiles represent.</param>
    /// <param name="onTileClicked">Receives every tile click.</param>
    public void BuildGrid(Board board, Action<Tile> onTileClicked)
    {
        Board = board;
        _onTileClicked = onTileClicked;
        GenerateGrid();
        Debug.Log($"Grid generated with dimensions {Board.Width} x {Board.Height}.");
    }

    /// <summary>Returns the tile at the given grid coordinate, or null if it is outside the grid.</summary>
    public Tile GetTile(Vector2Int coords)
    {
        _tiles.TryGetValue(coords, out Tile tile);
        return tile;
    }

    private void GenerateGrid()
    {
        for (int x = 0; x < Board.Width; x++)
        {
            for (int y = 0; y < Board.Height; y++)
            {
                Vector3 worldPos = new Vector3(x * tileSize, y * tileSize, 0);
                Tile spawnedTile = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                spawnedTile.name = $"Tile_{x}_{y}";
                spawnedTile.Init(x, y, this);

                _tiles[new Vector2Int(x, y)] = spawnedTile;
            }
        }
    }

    /// <summary>Returns the center of the grid in world space.</summary>
    public Vector3 GetGridCenter()
    {
        float centerX = (Board.Width * tileSize) / 2f - (tileSize / 2f);
        float centerY = (Board.Height * tileSize) / 2f - (tileSize / 2f);
        return new Vector3(centerX, centerY, 0f);
    }

    /// <summary>Links a unit's state to its view so tiles can report which view stands on them.</summary>
    public void RegisterUnitView(Unit unit) => _unitViews[unit.State] = unit;

    /// <summary>Forgets a unit's view, e.g. when it dies.</summary>
    public void UnregisterUnitView(Unit unit) => _unitViews.Remove(unit.State);

    /// <summary>Returns the view of the unit standing at the given cell, or null if it is empty.</summary>
    public Unit GetUnitView(Vector2Int coords) => GetUnitView(Board.GetUnitAt(coords));

    /// <summary>Returns the view of the given unit state, or null if it has none.</summary>
    public Unit GetUnitView(UnitState state) =>
        state != null && _unitViews.TryGetValue(state, out Unit unit) ? unit : null;

    /// <summary>Forwards a clicked tile to the callback given to <see cref="BuildGrid"/>.</summary>
    public void OnTileClicked(Tile clickedTile) => _onTileClicked?.Invoke(clickedTile);
}