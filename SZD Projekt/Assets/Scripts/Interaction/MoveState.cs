using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Default interaction state — highlights reachable tiles and handles movement.
/// Extracted from the original SelectionManager click/highlight logic.
/// </summary>
public class MoveState : IInteractionState
{
    private readonly SelectionManager _selectionManager;
    private readonly Match _match;
    private readonly GridManager _grid;
    private readonly List<Tile> _highlightedTiles = new List<Tile>();
    private Unit _unit;

    public MoveState(SelectionManager selectionManager, Match match, GridManager grid)
    {
        _selectionManager = selectionManager;
        _match = match;
        _grid = grid;
    }

    public void Enter(Unit selectedUnit)
    {
        _unit = selectedUnit;

        // Highlight the tile the unit is standing on
        _unit.CurrentTile.SetHighlight(TileHighlightState.Selected);

        // Highlight all valid move targets
        foreach (Vector2Int pos in _match.GetMoveTargets(_unit.State))
        {
            Tile tile = _grid.GetTile(pos);
            tile.SetHighlight(TileHighlightState.Reachable);
            _highlightedTiles.Add(tile);
        }
    }

    public void Exit()
    {
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
        Tile from = _unit.CurrentTile;
        if (_match.TryMove(_unit.State, clickedTile.Coords))
        {
            // The state has already moved, so CurrentTile is the new tile; clear the old one by hand
            from.SetHighlight(TileHighlightState.None);
            ClearHighlights();
            // The turn ends only once the animation (and pause) completes.
            _unit.MoveToTile(clickedTile, _match.EndTurn);
            _unit.CurrentTile.SetHighlight(TileHighlightState.Selected);
            _selectionManager.NotifySelectedUnitMoved(_unit);
            Debug.Log($"{_unit.name} moved to {_unit.CurrentTile.Coords}");
            return;
        }

        // Clicked empty non-reachable tile → deselect
        _selectionManager.Deselect();
    }

    // ─── Internal ────────────────────────────────────────────────────────────
    private void ClearHighlights()
    {
        if (_unit != null)
            _unit.CurrentTile.SetHighlight(TileHighlightState.None);

        foreach (Tile tile in _highlightedTiles)
            tile.SetHighlight(TileHighlightState.None);
        _highlightedTiles.Clear();
    }
}