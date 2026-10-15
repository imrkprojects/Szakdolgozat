using UnityEngine;
using System;
using System.Collections.Generic;

public class SelectionManager : MonoBehaviour
{   
    public static SelectionManager Instance { get; private set; }
    private Unit _selectedUnit;
    private List<Tile> _highlightedTiles = new List<Tile>();
    public static event Action<Unit> OnUnitSelected;
    public static event Action OnUnitDeselected;
    // Fired when the currently selected unit finishes a move
    public static event Action<Unit> OnSelectedUnitMoved;
    
    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        TurnManager.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        TurnManager.OnTurnChanged -= HandleTurnChanged;
    }
    
    public void HandleTileClicked(Tile clickedTile)
    {
        // Clicked a tile with a unit on it → only the active unit can be selected
        if (clickedTile.OccupyingUnit != null)
        {
            Unit unit = clickedTile.OccupyingUnit;
            if (unit != TurnManager.Instance.ActiveUnit)
            {
                Debug.Log($"Not your active unit's turn - active unit is {TurnManager.Instance.ActiveUnit?.name} ({TurnManager.Instance.ActivePlayer})");
                return;
            }
            Select(unit);
            return;
        }
        // A unit is selected and clicked a valid move tile → move then end the turn
        if (_selectedUnit != null && _selectedUnit.CanMoveTo(clickedTile))
        {
            _selectedUnit.MoveToTile(clickedTile);
            // HandleUnitMoved fires synchronously above (refreshes highlights, fires OnSelectedUnitMoved)
            TurnManager.Instance.EndTurn();
            // HandleTurnChanged fires synchronously above (deselects current, selects new active unit)
            return;
        }
        // Clicked empty non-reachable tile → clear selection
        if (_selectedUnit != null)
        {
            Deselect();
        }
    }

    // Auto-called when TurnManager switches to a new (player, unit) pair
    private void HandleTurnChanged(PlayerID player, Unit unit)
    {
        Deselect();
        if (unit != null) Select(unit);
    }
    
    private void Select(Unit unit)
    {
        // Clears if another unit is selected
        if (_selectedUnit != null) Deselect();
        _selectedUnit = unit;
        // Listens for movement while this unit is selected
        Unit.OnUnitMoved += HandleUnitMoved;
        // Highlights the tile the unit is standing on
        unit.CurrentTile.SetHighlight(TileHighlightState.Selected);
        // Highlights all valid move targets
        HighlightReachableTiles(unit);
        OnUnitSelected?.Invoke(unit);
        Debug.Log($"Selected {unit.name} at {unit.CurrentTile.Coords}");
    }
    
    private void Deselect()
    {
        if (_selectedUnit == null) return;
        Unit.OnUnitMoved -= HandleUnitMoved;
        ClearHighlights();
        _selectedUnit = null;
        OnUnitDeselected?.Invoke();
        Debug.Log("Deselected");
    }
    
    // Called when any unit moves but only acts on the currently selected unit (TBC)
    private void HandleUnitMoved(Unit unit)
    {
        if (unit != _selectedUnit) return; // TBC
        // Refresh highlights to reflect the new position
        ClearHighlights();
        unit.CurrentTile.SetHighlight(TileHighlightState.Selected);
        HighlightReachableTiles(unit);
        OnSelectedUnitMoved?.Invoke(unit);
        Debug.Log($"{unit.name} moved to {unit.CurrentTile.Coords}");
    }
    
    private void HighlightReachableTiles(Unit unit)
    {
        List<Tile> adjacent = GridManager.Instance.GetAdjacentTiles(unit.CurrentTile);
        foreach (Tile tile in adjacent)
        {
            if (unit.CanMoveTo(tile))
            {
                tile.SetHighlight(TileHighlightState.Reachable);
                _highlightedTiles.Add(tile);
            }
        }
    }

    private void ClearHighlights()
    {
        // Reset the unit's current tile highlight
        if (_selectedUnit != null)
            _selectedUnit.CurrentTile.SetHighlight(TileHighlightState.None);
        // Reset all reachable tile highlights
        foreach (Tile tile in _highlightedTiles)
            tile.SetHighlight(TileHighlightState.None);
        _highlightedTiles.Clear();
    }
}