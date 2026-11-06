using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Default interaction state — highlights reachable cells and handles movement.
/// Extracted from the original SelectionManager click/highlight logic.
/// </summary>
public class MoveState : IInteractionState
{
    private readonly SelectionManager _selectionManager;
    private readonly Match _match;
    private readonly GridManager _grid;
    private readonly List<Vector2Int> _highlightedCells = new List<Vector2Int>();
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

        // Highlight the cell the unit is standing on
        _grid.SetHighlight(_unit.State.Position, TileHighlightState.Selected);

        // Highlight all valid move targets
        foreach (Vector2Int pos in _match.GetMoveTargets(_unit.State))
        {
            _grid.SetHighlight(pos, TileHighlightState.Reachable);
            _highlightedCells.Add(pos);
        }

        Debug.Log($"Move mode — {_highlightedCells.Count} reachable cells");
    }

    public void Exit()
    {
        ClearHighlights();
        _unit = null;
    }

    public void HandleCellClicked(Vector2Int cell)
    {
        // Ignore input while the move animation plays
        if (_unit.IsMoving) return;

        Unit clicked = _grid.GetUnitView(cell);

        // Clicked the active unit's cell again — ignore (already selected)
        if (clicked == _unit) return;

        // Clicked another unit — can't select anything else during your turn
        if (clicked != null)
        {
            Debug.Log($"Not your active unit's turn — active unit is {_unit.name}");
            return;
        }

        // Clicked a valid move cell → move then end the turn
        Vector2Int from = _unit.State.Position;
        if (_match.TryMove(_unit.State, cell, out List<Vector2Int> path))
        {
            // The state has already moved, so State.Position is the new cell; clear the old one by hand
            _grid.SetHighlight(from, TileHighlightState.None);
            ClearHighlights();
            // The turn ends only once the animation (and pause) completes.
            _unit.MoveAlongPath(path, _match.EndTurn);
            _grid.SetHighlight(_unit.State.Position, TileHighlightState.Selected);
            _selectionManager.NotifySelectedUnitMoved(_unit);
            Debug.Log($"{_unit.name} moved to {_unit.State.Position}");
            return;
        }

        // Clicked empty non-reachable cell → deselect
        _selectionManager.Deselect();
    }

    // ─── Internal ────────────────────────────────────────────────────────────
    private void ClearHighlights()
    {
        if (_unit != null)
            _grid.SetHighlight(_unit.State.Position, TileHighlightState.None);

        foreach (Vector2Int cell in _highlightedCells)
            _grid.SetHighlight(cell, TileHighlightState.None);
        _highlightedCells.Clear();
    }
}