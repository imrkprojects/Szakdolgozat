using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ability targeting interaction state — highlights cells in ability range
/// and executes the ability when the player clicks a valid target.
/// </summary>
public class AbilityState : IInteractionState
{
    private readonly SelectionManager _selectionManager;
    private readonly Match _match;
    private readonly GridManager _grid;
    private readonly AbilityData _ability;
    private readonly List<Vector2Int> _highlightedCells = new List<Vector2Int>();
    private List<Vector2Int> _targetableCells;
    private Unit _unit;

    public AbilityState(SelectionManager selectionManager, Match match, GridManager grid, AbilityData ability)
    {
        _selectionManager = selectionManager;
        _match = match;
        _grid = grid;
        _ability = ability;
    }

    public void Enter(Unit selectedUnit)
    {
        _unit = selectedUnit;

        // Keep the unit's cell highlighted as selected
        _grid.SetHighlight(_unit.State.Position, TileHighlightState.Selected);

        // Get targetable cells from the ability's targeting rule
        _targetableCells = _match.GetAbilityTargets(_unit.State, _ability);

        // Highlight all cells in range
        foreach (Vector2Int cell in _targetableCells)
        {
            _grid.SetHighlight(cell, TileHighlightState.AbilityRange);
            _highlightedCells.Add(cell);
        }

        Debug.Log($"Ability mode: {_ability.abilityName} — {_targetableCells.Count} targetable cells");
    }

    public void Exit()
    {
        ClearHighlights();
        _targetableCells = null;
        _unit = null;
    }

    public void HandleCellClicked(Vector2Int cell)
    {
        // Ignore input while the ability (or a move) plays
        if (_unit.IsActing) return;

        // Clicked a cell within ability range → execute the ability
        if (_targetableCells != null && _targetableCells.Contains(cell))
        {
            // Match rejects cells the ability can't target — ignore the click so a misclick doesn't waste the turn,
            // except on the unit's own cell, which cancels back to move mode when it isn't a valid target
            if (!_match.TryUseAbility(_unit.State, _ability, cell))
            {
                if (cell == _unit.State.Position) _selectionManager.EnterMoveMode();
                return;
            }

            // Brief visual feedback — mark the target cell
            _grid.SetHighlight(cell, TileHighlightState.AbilityTarget);

            // Logged before ending the turn: that deselects, which clears _unit
            Debug.Log($"{_unit.name} used {_ability.abilityName} on cell {cell}");

            // Effects are already applied; end the turn once the ability has played — TurnCycle.TurnChanged
            // will fire and SelectionManager will deselect + select the next active unit automatically
            _unit.PlayAbility(_match.EndTurn);
            return;
        }

        // Clicked outside ability range → cancel back to move mode
        _selectionManager.EnterMoveMode();
    }

    private void ClearHighlights()
    {
        if (_unit != null)
            _grid.SetHighlight(_unit.State.Position, TileHighlightState.None);

        foreach (Vector2Int cell in _highlightedCells)
            _grid.SetHighlight(cell, TileHighlightState.None);
        _highlightedCells.Clear();
    }
}