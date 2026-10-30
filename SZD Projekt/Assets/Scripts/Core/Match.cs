using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>One match's game state: the players, the board, the turn order, and the rules that connect them.</summary>
public class Match
{
    public Board Board { get; }
    public TurnCycle Turns { get; } = new TurnCycle();
    public Player Player1 { get; } = new Player(PlayerID.Player1, "Player 1", Color.blue);
    public Player Player2 { get; } = new Player(PlayerID.Player2, "Player 2", Color.red);

    public Match(int width, int height)
    {
        Board = new Board(width, height);
    }

    /// <summary>Returns the player for an ID, or null if the ID is not a player.</summary>
    public Player GetPlayer(PlayerID id)
    {
        if (id == PlayerID.Player1) return Player1;
        if (id == PlayerID.Player2) return Player2;
        return null;
    }

    /// <summary>Puts a unit on the board, adds it to the turn order, and removes it from both when it dies.</summary>
    public void AddUnit(UnitState unit, Vector2Int pos)
    {
        Board.Place(unit, pos);
        Turns.RegisterUnit(unit);
        unit.Died += HandleUnitDied;
    }

    public void Start() => Turns.Start();

    /// <summary>Call when the active unit has finished its action (including its animation).</summary>
    public void EndTurn() => Turns.EndTurn();

    // ─── Movement ────────────────────────────────────────────────────────────

    /// <summary>Cells the unit could move to from where it stands.</summary>
    public List<Vector2Int> GetMoveTargets(UnitState unit) =>
        Board.GetAdjacent(unit.Position).FindAll(pos => Board.CanMoveTo(unit, pos));

    /// <summary>Moves the active unit if the move is legal. Does not end the turn.</summary>
    public bool TryMove(UnitState unit, Vector2Int to)
    {
        if (!CanAct(unit) || !Board.CanMoveTo(unit, to)) return false;
        Board.Place(unit, to);
        return true;
    }

    // ─── Abilities ───────────────────────────────────────────────────────────

    /// <summary>Cells the unit could aim the ability at.</summary>
    public List<Vector2Int> GetAbilityTargets(UnitState unit, AbilityData ability) =>
        ability.GetTargetablePositions(Board, unit);

    /// <summary>Uses one of the active unit's abilities on a cell if it is legal. Does not end the turn.</summary>
    public bool TryUseAbility(UnitState unit, AbilityData ability, Vector2Int target)
    {
        if (!CanAct(unit) || !unit.Abilities.Contains(ability)) return false;
        if (!GetAbilityTargets(unit, ability).Contains(target)) return false;

        // Only enemy-occupied cells are valid, so a misclick on an empty or friendly cell doesn't waste the turn
        UnitState occupant = Board.GetUnitAt(target);
        if (occupant == null || occupant.Owner == unit.Owner) return false;

        ability.Execute(this, unit, new[] { target });
        return true;
    }

    // ─── Internal ────────────────────────────────────────────────────────────

    private bool CanAct(UnitState unit) => !Turns.IsGameOver && unit == Turns.ActiveUnit;

    private void HandleUnitDied(UnitState unit)
    {
        unit.Died -= HandleUnitDied;
        Board.Remove(unit);
        Turns.UnregisterUnit(unit);
    }
}