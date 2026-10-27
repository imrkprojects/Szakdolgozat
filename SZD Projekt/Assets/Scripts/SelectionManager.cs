using UnityEngine;
using System;

/// <summary>Tracks the selected unit and routes tile clicks to the active interaction state.</summary>
public class SelectionManager : MonoBehaviour
{
    /// <summary>The active selection manager instance.</summary>
    public static SelectionManager Instance { get; private set; }

    private Unit _selectedUnit;
    private IInteractionState _currentState;

    // ─── Events ──────────────────────────────────────────────────────────────
    /// <summary>Raised when a unit becomes selected.</summary>
    public static event Action<Unit> OnUnitSelected;

    /// <summary>Raised when the current unit is deselected.</summary>
    public static event Action OnUnitDeselected;

    /// <summary>Raised when the selected unit moves.</summary>
    public static event Action<Unit> OnSelectedUnitMoved;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        TurnManager.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        TurnManager.OnTurnChanged -= HandleTurnChanged;
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>The currently selected unit, or null when nothing is selected.</summary>
    public Unit SelectedUnit => _selectedUnit;

    /// <summary>Processes a tile click using the current selection and interaction state.</summary>
    public void HandleTileClicked(Tile clickedTile)
    {
        if (TurnManager.Instance.IsGameOver) return;

        // No unit selected — only allow clicking the active unit to select it
        if (_selectedUnit == null)
        {
            if (clickedTile.OccupyingUnit != null
                && clickedTile.OccupyingUnit == TurnManager.Instance.ActiveUnit)
            {
                Select(clickedTile.OccupyingUnit);
            }

            return;
        }

        // A unit is selected — delegate the click to the current interaction state
        _currentState?.HandleTileClicked(clickedTile);
    }

    /// <summary>Switches the selected unit to its default movement mode.</summary>
    public void EnterMoveMode()
    {
        // Blocked mid-action: switching modes while a move or ability plays would show stale highlights
        if (_selectedUnit == null || _selectedUnit.IsActing) return;
        SetState(new MoveState(this));
    }

    /// <summary>
    /// Switches the selected unit to ability targeting mode.
    /// </summary>
    public void EnterAbilityMode(AbilityData ability)
    {
        // Blocked mid-action: the running action already ends the turn, so casting now would act twice
        if (_selectedUnit == null || ability == null || _selectedUnit.IsActing) return;
        SetState(new AbilityState(this, ability));
    }

    /// <summary>Deselects the current unit and exits its interaction state.</summary>
    public void Deselect()
    {
        if (_selectedUnit == null) return;
        _currentState?.Exit();
        _currentState = null;
        _selectedUnit = null;
        OnUnitDeselected?.Invoke();
        Debug.Log("Deselected");
    }

    /// <summary>
    /// Fire the OnSelectedUnitMoved event. Called by MoveState after a move.
    /// </summary>
    public void NotifySelectedUnitMoved(Unit unit)
    {
        OnSelectedUnitMoved?.Invoke(unit);
    }

    // ─── Internal ────────────────────────────────────────────────────────────

    private void Select(Unit unit)
    {
        if (_selectedUnit != null) Deselect();
        _selectedUnit = unit;
        OnUnitSelected?.Invoke(unit);
        Debug.Log($"Selected {unit.name} at {unit.CurrentTile.Coords}");

        // Default to move mode on selection
        SetState(new MoveState(this));
    }

    private void SetState(IInteractionState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter(_selectedUnit);
    }

    /// <summary>Selects the next active unit when the turn manager changes turns.</summary>
    private void HandleTurnChanged(PlayerID player, Unit unit)
    {
        Deselect(); // Safety net
        if (unit != null) Select(unit);
    }
}