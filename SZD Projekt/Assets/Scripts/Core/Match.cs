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
    public void EndTurn()
    {
        // A pass is a turn too, so cooldowns tick whether or not the unit acted
        Turns.ActiveUnit?.TickCooldowns();
        Turns.EndTurn();
    }

    // ─── Movement ────────────────────────────────────────────────────────────

    /// <summary>Cells the unit could move to from where it stands, within its move range.</summary>
    public List<Vector2Int> GetMoveTargets(UnitState unit) => Board.GetReachable(unit, unit.MoveRange);

    /// <summary>
    /// Moves the active unit if the move is legal and returns the cells it walked through (ending at
    /// <paramref name="to"/>) for the view to animate. Does not end the turn.
    /// </summary>
    public bool TryMove(UnitState unit, Vector2Int to, out List<Vector2Int> path)
    {
        path = null;
        if (!CanAct(unit)) return false;
        path = Board.FindPath(unit, to, unit.MoveRange);
        if (path == null) return false;
        Turns.BeginAction();
        Board.Place(unit, to);
        return true;
    }

    // ─── Abilities ───────────────────────────────────────────────────────────

    /// <summary>Cells the unit could aim the ability at.</summary>
    public List<Vector2Int> GetAbilityTargets(UnitState unit, AbilityData ability) =>
        ability.GetTargetablePositions(Board, unit);

    /// <summary>True if the unit may use the ability now (it owns it, may act, and it is off cooldown), ignoring targets.</summary>
    public bool CanUseAbility(UnitState unit, AbilityData ability) =>
        ability != null && CanAct(unit) && unit.Abilities.Contains(ability) && unit.GetCooldown(ability) == 0;

    /// <summary>Uses one of the active unit's abilities on a cell if it is legal. Does not end the turn.</summary>
    public bool TryUseAbility(UnitState unit, AbilityData ability, Vector2Int target)
    {
        if (!CanUseAbility(unit, ability)) return false;
        if (!GetAbilityTargets(unit, ability).Contains(target)) return false;

        // A cell the ability can't affect is rejected, so a misclick doesn't waste the turn
        if (!ability.IsValidTarget(Board, unit, target)) return false;

        // Before the effects, so a death caused by this action is recorded after the unit left its queue
        Turns.BeginAction();
        ability.Execute(this, unit, new[] { target });
        unit.StartCooldown(ability);
        return true;
    }

    // ─── Internal ────────────────────────────────────────────────────────────

    // ActiveUnit is null outside Acting; a unit that has started gets no second action before EndTurn
    private bool CanAct(UnitState unit) => unit != null && unit == Turns.ActiveUnit && !Turns.HasActiveUnitStarted;

    private void HandleUnitDied(UnitState unit)
    {
        unit.Died -= HandleUnitDied;
        Board.Remove(unit);
        Turns.UnregisterUnit(unit);
    }
}