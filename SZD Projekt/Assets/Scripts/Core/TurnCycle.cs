using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs the turn order: both players plan their unit order once, then turns strictly alternate P1, P2, P1, ...
/// Each player works through their own queue; when it runs out, their next turn starts a new cycle of their order.
/// A player may reorder on their turn before acting: free after one of their units died, or for a token at the
/// start of their cycle. A reorder starts a new cycle at once.
/// </summary>
public class TurnCycle
{
    private const int TokensPerMatch = 2;
    private static readonly PlayerID[] Players = { PlayerID.Player1, PlayerID.Player2 };

    // Living units in their owner's order; each new cycle is a copy of it
    private readonly Dictionary<PlayerID, List<UnitState>> _roster = new Dictionary<PlayerID, List<UnitState>>
    {
        { PlayerID.Player1, new List<UnitState>() },
        { PlayerID.Player2, new List<UnitState>() }
    };

    // What is left of the player's current cycle
    private readonly Dictionary<PlayerID, List<UnitState>> _queue = new Dictionary<PlayerID, List<UnitState>>
    {
        { PlayerID.Player1, new List<UnitState>() },
        { PlayerID.Player2, new List<UnitState>() }
    };

    private readonly Dictionary<PlayerID, int> _tokens = new Dictionary<PlayerID, int>();
    private readonly Dictionary<PlayerID, int> _cycles = new Dictionary<PlayerID, int>();
    private readonly HashSet<PlayerID> _committed = new HashSet<PlayerID>();

    // Owners with a free reorder from a death, offered on their next turn before they act
    private readonly HashSet<PlayerID> _deathReorders = new HashSet<PlayerID>();

    // The active player's queue was refilled at the start of this turn, so a token reorder is offered
    private bool _atCycleStart;

    public CyclePhase Phase { get; private set; } = CyclePhase.Planning;

    /// <summary>Player whose turn it is; None during Planning.</summary>
    public PlayerID ActivePlayer { get; private set; } = PlayerID.None;

    /// <summary>Unit that may act now. Null outside <see cref="CyclePhase.Acting"/>.</summary>
    public UnitState ActiveUnit { get; private set; }

    public bool IsGameOver => Phase == CyclePhase.GameOver;

    /// <summary>Whether the active unit has begun its action (it has left the queue).</summary>
    public bool HasActiveUnitStarted => ActiveUnit != null && !_queue[ActivePlayer].Contains(ActiveUnit);

    /// <summary>Raised when a new unit becomes the active unit, including after a reorder.</summary>
    public event Action<PlayerID, UnitState> TurnChanged;

    /// <summary>Raised once when a player has no units left; carries the winner, or None for a draw.</summary>
    public event Action<PlayerID> GameOver;

    /// <summary>Raised on entering a phase.</summary>
    public event Action<CyclePhase> PhaseChanged;

    /// <summary>The player's living units in their order; the next cycle follows it.</summary>
    public IReadOnlyList<UnitState> GetLivingUnits(PlayerID player) => _roster[player];

    /// <summary>The rest of the player's current cycle, including the active unit until it starts.</summary>
    public IReadOnlyList<UnitState> GetQueue(PlayerID player) => _queue[player];

    public bool HasCommitted(PlayerID player) => _committed.Contains(player);

    public int GetTokens(PlayerID player) => _tokens.TryGetValue(player, out int tokens) ? tokens : 0;

    /// <summary>The player's own 1-based cycle number; 0 before acting starts.</summary>
    public int GetCycle(PlayerID player) => _cycles.TryGetValue(player, out int cycle) ? cycle : 0;

    /// <summary>Adds a unit to the end of its owner's order. Call before <see cref="Start"/>.</summary>
    public void RegisterUnit(UnitState unit)
    {
        if (_roster.TryGetValue(unit.Owner, out List<UnitState> units)) units.Add(unit);
    }

    /// <summary>Starts the match: tokens reset, planning opens.</summary>
    public void Start()
    {
        foreach (PlayerID player in Players) _tokens[player] = TokensPerMatch;
        if (TryEndGame()) return;
        SetPhase(CyclePhase.Planning);
    }

    /// <summary>Locks in a player's first order. Player 1's turn starts once both players have committed.</summary>
    public bool CommitPlan(PlayerID player, IReadOnlyList<UnitState> order)
    {
        if (Phase != CyclePhase.Planning || !_roster.ContainsKey(player) || _committed.Contains(player)) return false;
        if (!IsPermutation(order, _roster[player])) return false;

        _roster[player] = new List<UnitState>(order);
        _committed.Add(player);
        Debug.Log($"{player} committed their order");
        if (_committed.Count < Players.Length) return true;

        // Filled here, so the first turns are not cycle starts and offer no token
        foreach (PlayerID p in Players) StartCycle(p);
        StartTurn(PlayerID.Player1);
        return true;
    }

    /// <summary>Takes the active unit out of its queue. Match calls this when an action succeeds.</summary>
    public void BeginAction()
    {
        if (Phase != CyclePhase.Acting || ActiveUnit == null || HasActiveUnitStarted) return;
        _queue[ActivePlayer].Remove(ActiveUnit);
        // Acting uses up this turn's reorder offers; a death caused by this action is recorded after this
        _deathReorders.Remove(ActivePlayer);
        _atCycleStart = false;
    }

    /// <summary>Removes a dead unit from its owner's order and queue, and gives the owner a free reorder.</summary>
    public void UnregisterUnit(UnitState unit)
    {
        if (!_roster.TryGetValue(unit.Owner, out List<UnitState> units) || !units.Remove(unit)) return;
        _queue[unit.Owner].Remove(unit);
        _deathReorders.Add(unit.Owner);
    }

    /// <summary>Call when the active unit has finished its action. Without an action it counts as a pass.</summary>
    public void EndTurn()
    {
        if (Phase != CyclePhase.Acting) return;

        BeginAction();
        ActiveUnit = null;
        if (TryEndGame()) return;
        StartTurn(Other(ActivePlayer));
    }

    /// <summary>Whether the player may reorder now: their turn, before acting, with ≥2 living units, and free or paid.</summary>
    public bool CanReorder(PlayerID player) =>
        Phase == CyclePhase.Acting && player == ActivePlayer && !HasActiveUnitStarted && _roster[player].Count > 1
        && (ReorderIsFree(player) || (_atCycleStart && GetTokens(player) > 0));

    /// <summary>Whether the player's reorder is free because one of their units died.</summary>
    public bool ReorderIsFree(PlayerID player) => _deathReorders.Contains(player);

    /// <summary>Sets a new order for all living units and starts a new cycle with it at once. Costs a token unless free.</summary>
    public bool TryReorder(PlayerID player, IReadOnlyList<UnitState> order)
    {
        if (!CanReorder(player) || !IsPermutation(order, _roster[player])) return false;

        if (!_deathReorders.Remove(player)) _tokens[player]--;
        // Copy: order may be the list GetLivingUnits returned
        _roster[player] = new List<UnitState>(order);
        // At a cycle start nothing has acted yet, so the new order replaces that cycle instead of adding one
        if (_atCycleStart) _queue[player] = new List<UnitState>(order);
        else StartCycle(player);
        // One reorder per turn, and the cycle it starts did not end, so no token offer follows
        _atCycleStart = false;
        ActiveUnit = _queue[player][0];
        Debug.Log($"{player} reordered and started cycle {GetCycle(player)}");
        TurnChanged?.Invoke(ActivePlayer, ActiveUnit);
        return true;
    }

    // ─── Internal ────────────────────────────────────────────────────────────

    private void StartTurn(PlayerID player)
    {
        ActivePlayer = player;
        _atCycleStart = _queue[player].Count == 0;
        if (_atCycleStart) StartCycle(player);

        ActiveUnit = _queue[player][0];
        if (Phase != CyclePhase.Acting) SetPhase(CyclePhase.Acting);
        TurnChanged?.Invoke(ActivePlayer, ActiveUnit);
        Debug.Log($"Turn → {player} (cycle {GetCycle(player)}) acts with the unit at {ActiveUnit.Position}");
    }

    private void StartCycle(PlayerID player)
    {
        _queue[player] = new List<UnitState>(_roster[player]);
        _cycles[player] = GetCycle(player) + 1;
    }

    private bool TryEndGame()
    {
        bool p1Out = _roster[PlayerID.Player1].Count == 0;
        bool p2Out = _roster[PlayerID.Player2].Count == 0;
        if (!p1Out && !p2Out) return false;

        PlayerID winner = p1Out && p2Out ? PlayerID.None : p1Out ? PlayerID.Player2 : PlayerID.Player1;
        ActiveUnit = null;
        SetPhase(CyclePhase.GameOver);
        Debug.Log(winner == PlayerID.None ? "Game over — draw" : $"Game over — {winner} wins");
        GameOver?.Invoke(winner);
        return true;
    }

    private void SetPhase(CyclePhase phase)
    {
        Phase = phase;
        PhaseChanged?.Invoke(phase);
    }

    private static PlayerID Other(PlayerID player) =>
        player == PlayerID.Player1 ? PlayerID.Player2 : PlayerID.Player1;

    // Same units, each exactly once
    private static bool IsPermutation(IReadOnlyList<UnitState> order, List<UnitState> units)
    {
        if (order == null || order.Count != units.Count) return false;
        var set = new HashSet<UnitState>(order);
        return set.Count == order.Count && set.SetEquals(units);
    }
}