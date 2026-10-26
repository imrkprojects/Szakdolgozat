using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Default interaction state — highlights reachable tiles and handles movement.
/// Extracted from the original SelectionManager click/highlight logic.
/// </summary>
public class MoveState : IInteractionState
{
    private readonly SelectionManager _selectionManager;
    private readonly List<Tile> _highlightedTiles = new List<Tile>();
    private Unit _unit;

    public MoveState(SelectionManager selectionManager)
    {
        _selectionManager = selectionManager;
    }

    public void Enter(Unit selectedUnit)
    {
        _unit = selectedUnit;

        // Highlight the tile the unit is standing on
        _unit.CurrentTile.SetHighlight(TileHighlightState.Selected);

        // Highlight all valid move targets
        List<Tile> adjacent = GridManager.Instance.GetAdjacentTiles(_unit.CurrentTile);
        foreach (Tile tile in adjacent)
        {
            if (_unit.CanMoveTo(tile))
            {
                tile.SetHighlight(TileHighlightState.Reachable);
                _highlightedTiles.Add(tile);
            }
        }

        // Listen for movement so we can refresh highlights after a move
        Unit.OnUnitMoved += HandleUnitMoved;
    }

    public void Exit()
    {
        Unit.OnUnitMoved -= HandleUnitMoved;
        ClearHighlights();
        _unit = null;
    }

    public void HandleTileClicked(Tile clickedTile)
    {
        // Ignore input while the move animation plays
        if (_unit.IsMoving) return;

        // Clicked the active unit's tile again — ignore (already selected)
        if (clickedTile.OccupyingUnit == _unit) return;

        // Clicked another unit — can't select anything else during your turn
        if (clickedTile.OccupyingUnit != null)
        {
            Debug.Log($"Not your active unit's turn — active unit is {_unit.name}");
            return;
        }

        // Clicked a valid move tile → move then end the turn
        if (_unit.CanMoveTo(clickedTile))
        {
            // HandleUnitMoved fires synchronously (refreshes highlights, fires OnSelectedUnitMoved);
            // the turn ends only once the animation (and pause) completes.
            _unit.MoveToTile(clickedTile, () => TurnManager.Instance.EndTurn());
            return;
        }

        // Clicked empty non-reachable tile → deselect
        _selectionManager.Deselect();
    }

    // ─── Internal ────────────────────────────────────────────────────────────
    private void HandleUnitMoved(Unit unit)
    {
        if (unit != _unit) return;

        // Refresh highlights to reflect the new position
        ClearHighlights();
        _unit.CurrentTile.SetHighlight(TileHighlightState.Selected);

        List<Tile> adjacent = GridManager.Instance.GetAdjacentTiles(_unit.CurrentTile);
        foreach (Tile tile in adjacent)
        {
            if (_unit.CanMoveTo(tile))
            {
                tile.SetHighlight(TileHighlightState.Reachable);
                _highlightedTiles.Add(tile);
            }
        }

        _selectionManager.NotifySelectedUnitMoved(unit);
        Debug.Log($"{unit.name} moved to {unit.CurrentTile.Coords}");
    }

    private void ClearHighlights()
    {
        if (_unit != null)
            _unit.CurrentTile.SetHighlight(TileHighlightState.None);

        foreach (Tile tile in _highlightedTiles)
            tile.SetHighlight(TileHighlightState.None);
        _highlightedTiles.Clear();
    }
}