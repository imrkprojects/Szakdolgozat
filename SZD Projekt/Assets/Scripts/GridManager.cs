using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System;
using System.Collections.Generic;

/// <summary>Draws the match grid, its highlights and hover marker, and turns mouse clicks into cell clicks.</summary>
public class GridManager : MonoBehaviour
{
    private const int MinimumDimension = 3;

    [Header("Grid Dimensions")] [Min(MinimumDimension)] [SerializeField]
    private int width = 20;

    [Min(MinimumDimension)] [SerializeField]
    private int height = 10;

    [SerializeField] private float tileSize = 1.1f; // 1.0 + slight spacing

    [Header("Scene")] [SerializeField] private Grid grid;
    [SerializeField] private Tilemap groundMap;
    [SerializeField] private Tilemap highlightMap;
    [SerializeField] private SpriteRenderer hoverMarker;
    [SerializeField] private Camera cam;

    [Header("Tiles")] [SerializeField] private TileBase groundTile; // grey square
    [SerializeField] private TileBase highlightTile; // plain white square, tinted per cell

    [SerializeField] private Color reachableColor = new Color(0.2f, 0.7f, 0.2f, 0.75f);
    [SerializeField] private Color selectedColor = new Color(0.9f, 0.85f, 0.1f, 0.75f);
    [SerializeField] private Color abilityRangeColor = new Color(1f, 0.55f, 0.1f, 0.75f);
    [SerializeField] private Color abilityTargetColor = new Color(0.9f, 0.1f, 0.1f, 0.75f);

    private readonly Dictionary<Vector2Int, TileHighlightState> _highlights =
        new Dictionary<Vector2Int, TileHighlightState>();

    private readonly Dictionary<UnitState, Unit> _unitViews = new Dictionary<UnitState, Unit>();
    private Action<Vector2Int> _onCellClicked;

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
        hoverMarker.enabled = false;
    }

    private void ClampDimensions()
    {
        width = Mathf.Max(MinimumDimension, width);
        height = Mathf.Max(MinimumDimension, height);
    }

    /// <summary>Paints the ground for every board cell.</summary>
    /// <param name="board">Game state the grid represents.</param>
    /// <param name="onCellClicked">Receives the cell of every board click.</param>
    public void BuildGrid(Board board, Action<Vector2Int> onCellClicked)
    {
        Board = board;
        _onCellClicked = onCellClicked;
        // Cell (x, y) is centered at (x, y) * tileSize; set here so the scene can't drift from tileSize
        grid.cellSize = new Vector3(1f, 1f, 0f); // z 0 keeps cell centers on the z = 0 plane
        grid.cellGap = new Vector3(tileSize - 1f, tileSize - 1f, 0f);
        grid.transform.position = new Vector3(-0.5f, -0.5f, 0f);
        GenerateGrid();
        Debug.Log($"Grid generated with dimensions {Board.Width} x {Board.Height}.");
    }

    /// <summary>World position of the center of the given cell.</summary>
    public Vector3 CellToWorld(Vector2Int cell) => grid.GetCellCenterWorld((Vector3Int)cell);

    /// <summary>Shows the given highlight on a cell.</summary>
    public void SetHighlight(Vector2Int cell, TileHighlightState state)
    {
        var pos = (Vector3Int)cell;
        if (state == TileHighlightState.None)
        {
            highlightMap.SetTile(pos, null);
            _highlights.Remove(cell);
            return;
        }

        highlightMap.SetTile(pos, highlightTile);
        highlightMap.SetTileFlags(pos, TileFlags.None); // otherwise the tile's locked color ignores SetColor
        highlightMap.SetColor(pos, HighlightColor(state));
        _highlights[cell] = state;
    }

    /// <summary>The highlight currently shown on a cell.</summary>
    public TileHighlightState GetHighlight(Vector2Int cell) =>
        _highlights.TryGetValue(cell, out TileHighlightState state) ? state : TileHighlightState.None;

    private Color HighlightColor(TileHighlightState state) => state switch
    {
        TileHighlightState.Reachable => reachableColor,
        TileHighlightState.Selected => selectedColor,
        TileHighlightState.AbilityRange => abilityRangeColor,
        _ => abilityTargetColor
    };

    private void Update()
    {
        if (Board == null || Mouse.current == null) return;

        Vector3 world = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        var cell = (Vector2Int)grid.WorldToCell(world);
        bool inBounds = Board.InBounds(cell);

        // Hover lightens only plain, reachable and in-range cells, as the old tile colors did
        TileHighlightState highlight = GetHighlight(cell);
        hoverMarker.enabled = inBounds && (highlight == TileHighlightState.None
                                           || highlight == TileHighlightState.Reachable
                                           || highlight == TileHighlightState.AbilityRange);
        if (hoverMarker.enabled) hoverMarker.transform.position = CellToWorld(cell);

        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        // A click on a uGUI element over the grid is not a board click
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (inBounds) _onCellClicked?.Invoke(cell);
    }

    private void GenerateGrid()
    {
        for (int x = 0; x < Board.Width; x++)
        {
            for (int y = 0; y < Board.Height; y++)
                groundMap.SetTile(new Vector3Int(x, y, 0), groundTile);
        }
    }

    /// <summary>Returns the center of the grid in world space.</summary>
    public Vector3 GetGridCenter()
    {
        float centerX = (Board.Width * tileSize) / 2f - (tileSize / 2f);
        float centerY = (Board.Height * tileSize) / 2f - (tileSize / 2f);
        return new Vector3(centerX, centerY, 0f);
    }

    /// <summary>Links a unit's state to its view so a cell can report which view stands on it.</summary>
    public void RegisterUnitView(Unit unit) => _unitViews[unit.State] = unit;

    /// <summary>Forgets a unit's view, e.g. when it dies.</summary>
    public void UnregisterUnitView(Unit unit) => _unitViews.Remove(unit.State);

    /// <summary>Returns the view of the unit standing at the given cell, or null if it is empty.</summary>
    public Unit GetUnitView(Vector2Int coords) => GetUnitView(Board.GetUnitAt(coords));

    /// <summary>Returns the view of the given unit state, or null if it has none.</summary>
    public Unit GetUnitView(UnitState state) =>
        state != null && _unitViews.TryGetValue(state, out Unit unit) ? unit : null;
}