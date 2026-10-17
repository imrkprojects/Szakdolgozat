using UnityEngine;
using System;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    // Units stored in registration order. Null slots = dead units (preserves indices)
    private List<Unit> _player1Units = new List<Unit>();
    private List<Unit> _player2Units = new List<Unit>();

    // Each player's index advances independently each time they act
    private int _p1UnitIndex;
    private int _p2UnitIndex;

    public PlayerID ActivePlayer { get; private set; } = PlayerID.Player1;
    // The specific unit that must act this turn
    public Unit ActiveUnit { get; private set; }

    // Fired when control passes to a new (player, unit) pair
    public static event Action<PlayerID, Unit> OnTurnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartGame()
    {
        ActivePlayer  = PlayerID.Player1;
        _p1UnitIndex  = 0;
        _p2UnitIndex  = 0;
        ActiveUnit    = GetCurrentUnit(PlayerID.Player1);
        OnTurnChanged?.Invoke(ActivePlayer, ActiveUnit);
        Debug.Log($"Game started — {ActivePlayer} acts with {ActiveUnit?.name}");
    }

    public void RegisterUnit(Unit unit)
    {
        if      (unit.OwnerID == PlayerID.Player1) _player1Units.Add(unit);
        else if (unit.OwnerID == PlayerID.Player2) _player2Units.Add(unit);
    }

    // Called when a unit is removed from the game (death, capture, etc.)
    public void UnregisterUnit(Unit unit)
    {
        // Null the slot so the cycle skips it and list size stays the same (preserves modular indexing)
        int idx = _player1Units.IndexOf(unit);
        if (idx >= 0) { _player1Units[idx] = null; return; }
        idx = _player2Units.IndexOf(unit);
        if (idx >= 0)  { _player2Units[idx] = null; }
    }

    // Called by SelectionManager after the active unit makes a move
    public void EndTurn()
    {
        // Advance the acting player's index so their next turn uses the next unit (TBC)
        AdvanceIndex(ActivePlayer);

        // Alternate the active player
        ActivePlayer = ActivePlayer == PlayerID.Player1 ? PlayerID.Player2 : PlayerID.Player1;

        // Find the next living unit for the new active player
        ActiveUnit = GetCurrentUnit(ActivePlayer);

        if (ActiveUnit == null)
        {
            Debug.Log($"{ActivePlayer} has no units left!");
            // TODO: fire a game-over event
            return;
        }

        OnTurnChanged?.Invoke(ActivePlayer, ActiveUnit);
        Debug.Log($"Turn → {ActivePlayer} acts with {ActiveUnit.name}");
    }

    // Returns the unit at the current index, skipping null (dead) slots.
    // Also updates the stored index to point at the first living unit found.
    private Unit GetCurrentUnit(PlayerID player)
    {
        List<Unit> units = player == PlayerID.Player1 ? _player1Units : _player2Units;
        int start        = player == PlayerID.Player1 ? _p1UnitIndex  : _p2UnitIndex;

        for (int i = 0; i < units.Count; i++)
        {
            int try_ = (start + i) % units.Count;
            if (units[try_] != null)
            {
                // Commit the index so future AdvanceIndex starts from here
                if (player == PlayerID.Player1) _p1UnitIndex = try_;
                else                            _p2UnitIndex = try_;
                return units[try_];
            }
        }
        return null; // All units dead
    }

    // Moves the index forward by one position (wraps around)
    private void AdvanceIndex(PlayerID player)
    {
        List<Unit> units = player == PlayerID.Player1 ? _player1Units : _player2Units;
        if (units.Count == 0) return;

        int current = player == PlayerID.Player1 ? _p1UnitIndex : _p2UnitIndex;
        int next    = (current + 1) % units.Count;

        if (player == PlayerID.Player1) _p1UnitIndex = next;
        else                            _p2UnitIndex = next;
    }
}
