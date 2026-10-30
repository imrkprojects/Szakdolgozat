using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Tracks unit turn order, advances turns, and reports match completion.</summary>
public class TurnCycle
{
    // Units stored in registration order. Null slots = dead units (preserves indices)
    private readonly List<UnitState> _player1Units = new List<UnitState>();
    private readonly List<UnitState> _player2Units = new List<UnitState>();

    // Each player's index advances independently each time they act
    private int _p1UnitIndex;
    private int _p2UnitIndex;

    /// <summary>Player whose turn is currently active.</summary>
    public PlayerID ActivePlayer { get; private set; } = PlayerID.Player1;

    /// <summary>Unit that must act during the current turn.</summary>
    public UnitState ActiveUnit { get; private set; }

    /// <summary>Whether the match has ended.</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>Raised when control passes to a new player and active unit.</summary>
    public event Action<PlayerID, UnitState> TurnChanged;

    /// <summary>Raised once when a player has no units left; carries the winner's ID.</summary>
    public event Action<PlayerID> GameOver;

    /// <summary>Resets turn order and selects Player 1's first living unit.</summary>
    public void Start()
    {
        ActivePlayer = PlayerID.Player1;
        _p1UnitIndex = 0;
        _p2UnitIndex = 0;
        ActiveUnit = GetCurrentUnit(PlayerID.Player1);
        TurnChanged?.Invoke(ActivePlayer, ActiveUnit);
        Debug.Log($"Game started — {ActivePlayer} acts with the unit at {ActiveUnit?.Position}");
    }

    /// <summary>Adds a unit to its owner's turn order.</summary>
    public void RegisterUnit(UnitState unit)
    {
        if (unit.Owner == PlayerID.Player1) _player1Units.Add(unit);
        else if (unit.Owner == PlayerID.Player2) _player2Units.Add(unit);
    }

    /// <summary>Marks a unit's turn-order slot empty without shifting later indices.</summary>
    public void UnregisterUnit(UnitState unit)
    {
        // Null the slot so the cycle skips it and list size stays the same (preserves modular indexing)
        int idx = _player1Units.IndexOf(unit);
        if (idx >= 0)
        {
            _player1Units[idx] = null;
            return;
        }

        idx = _player2Units.IndexOf(unit);
        if (idx >= 0)
        {
            _player2Units[idx] = null;
        }
    }

    /// <summary>Ends the active turn, advances to the next player, and checks for a winner.</summary>
    public void EndTurn()
    {
        if (IsGameOver) return;

        // Advance the acting player's index so their next turn uses the next unit (TBC)
        AdvanceIndex(ActivePlayer);

        // Alternate the active player
        ActivePlayer = ActivePlayer == PlayerID.Player1 ? PlayerID.Player2 : PlayerID.Player1;

        // Find the next living unit for the new active player
        ActiveUnit = GetCurrentUnit(ActivePlayer);

        if (ActiveUnit == null)
        {
            Debug.Log($"{ActivePlayer} has no units left!");
            IsGameOver = true;
            // The player who just acted wins
            GameOver?.Invoke(ActivePlayer == PlayerID.Player1 ? PlayerID.Player2 : PlayerID.Player1);
            return;
        }

        TurnChanged?.Invoke(ActivePlayer, ActiveUnit);
        Debug.Log($"Turn → {ActivePlayer} acts with the unit at {ActiveUnit.Position}");
    }

    // Returns the unit at the current index, skipping null (dead) slots.
    // Also updates the stored index to point at the first living unit found.
    private UnitState GetCurrentUnit(PlayerID player)
    {
        List<UnitState> units = player == PlayerID.Player1 ? _player1Units : _player2Units;
        int start = player == PlayerID.Player1 ? _p1UnitIndex : _p2UnitIndex;

        for (int i = 0; i < units.Count; i++)
        {
            int try_ = (start + i) % units.Count;
            if (units[try_] != null)
            {
                // Commit the index so future AdvanceIndex starts from here
                if (player == PlayerID.Player1) _p1UnitIndex = try_;
                else _p2UnitIndex = try_;
                return units[try_];
            }
        }

        return null; // All units dead
    }

    // Moves the index forward by one position (wraps around)
    private void AdvanceIndex(PlayerID player)
    {
        List<UnitState> units = player == PlayerID.Player1 ? _player1Units : _player2Units;
        if (units.Count == 0) return;

        int current = player == PlayerID.Player1 ? _p1UnitIndex : _p2UnitIndex;
        int next = (current + 1) % units.Count;

        if (player == PlayerID.Player1) _p1UnitIndex = next;
        else _p2UnitIndex = next;
    }
}
