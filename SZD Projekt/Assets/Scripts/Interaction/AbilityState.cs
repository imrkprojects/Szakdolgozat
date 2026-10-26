using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ability targeting interaction state — highlights tiles in ability range
/// and executes the ability when the player clicks a valid target.
/// </summary>
public class AbilityState : IInteractionState
{
    private readonly SelectionManager _selectionManager;
    private readonly AbilityData _ability;
    private readonly List<Tile> _highlightedTiles = new List<Tile>();
    private List<Tile> _targetableTiles;
    private Unit _unit;

    public AbilityState(SelectionManager selectionManager, AbilityData ability)
    {
        _selectionManager = selectionManager;
        _ability = ability;
    }

    public void Enter(Unit selectedUnit)
    {
        _unit = selectedUnit;

        // Keep the unit's tile highlighted as selected
        _unit.CurrentTile.SetHighlight(TileHighlightState.Selected);

        // Get targetable tiles from the ability's targeting rule
        _targetableTiles = _ability.GetTargetableTiles(_unit);

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
        // Clicked the unit's own tile → cancel back to move mode
        if (clickedTile == _unit.CurrentTile)
        {
            _selectionManager.EnterMoveMode();
            return;
        }

        // Clicked a tile within ability range → execute the ability
        if (_targetableTiles != null && _targetableTiles.Contains(clickedTile))
        {
            // Brief visual feedback — mark the target tile
            clickedTile.SetHighlight(TileHighlightState.AbilityTarget);

            // Execute all effects on the clicked tile
            _ability.Execute(_unit, new List<Tile> { clickedTile });
            Debug.Log($"{_unit.name} used {_ability.abilityName} on tile {clickedTile.Coords}");

            // End the turn — TurnManager.OnTurnChanged will fire and SelectionManager
            // will deselect + select the next active unit automatically
            TurnManager.Instance.EndTurn();
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
