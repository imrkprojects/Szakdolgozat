using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ability targeting interaction state — highlights tiles in ability range
/// and executes the ability when the player clicks a valid target.
/// </summary>
public class AbilityState : IInteractionState
{
    private readonly SelectionManager _selectionManager;
    private readonly Match _match;
    private readonly GridManager _grid;
    private readonly AbilityData _ability;
    private readonly List<Tile> _highlightedTiles = new List<Tile>();
    private List<Tile> _targetableTiles;
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

        // Keep the unit's tile highlighted as selected
        _unit.CurrentTile.SetHighlight(TileHighlightState.Selected);

        // Get targetable tiles from the ability's targeting rule
        _targetableTiles = _match.GetAbilityTargets(_unit.State, _ability).ConvertAll(_grid.GetTile);

        // Highlight all tiles in range
        foreach (Tile tile in _targetableTiles)
        {
            tile.SetHighlight(TileHighlightState.AbilityRange);
            _highlightedTiles.Add(tile);
        }

        Debug.Log($"Ability mode: {_ability.abilityName} — {_targetableTiles.Count} targetable tiles");
    }

    public void Exit()
    {
        ClearHighlights();
        _targetableTiles = null;
        _unit = null;
    }

    public void HandleTileClicked(Tile clickedTile)
    {
        // Ignore input while the ability (or a move) plays
        if (_unit.IsActing) return;

        // Clicked the unit's own tile → cancel back to move mode
        if (clickedTile == _unit.CurrentTile)
        {
            _selectionManager.EnterMoveMode();
            return;
        }

        // Clicked a tile within ability range → execute the ability
        if (_targetableTiles != null && _targetableTiles.Contains(clickedTile))
        {
            // Match rejects empty or friendly tiles — ignore the click so a misclick doesn't waste the turn
            if (!_match.TryUseAbility(_unit.State, _ability, clickedTile.Coords)) return;

            // Brief visual feedback — mark the target tile
            clickedTile.SetHighlight(TileHighlightState.AbilityTarget);

            // Logged before ending the turn: that deselects, which clears _unit
            Debug.Log($"{_unit.name} used {_ability.abilityName} on tile {clickedTile.Coords}");

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
            _unit.CurrentTile.SetHighlight(TileHighlightState.None);

        foreach (Tile tile in _highlightedTiles)
            tile.SetHighlight(TileHighlightState.None);
        _highlightedTiles.Clear();
    }
}