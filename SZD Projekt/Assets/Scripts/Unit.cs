using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>Scene view of a unit: sprite, label and move animation. Game state lives in <see cref="State"/>.</summary>
public class Unit : MonoBehaviour
{
    public int unitID;

    // ─── Appearance ──────────────────────────────────────────────────────────
    [SerializeField] private Color unitColor;
    private SpriteRenderer _spriteRenderer;

    // ─── State ───────────────────────────────────────────────────────────────
    private GridManager _grid;

    /// <summary>Game state of this unit; created by <see cref="Init"/>.</summary>
    public UnitState State { get; private set; }

    public PlayerID OwnerID => State.Owner;

    // ─── Position ────────────────────────────────────────────────────────────
    public Tile CurrentTile => _grid.GetTile(State.Position);

    // ─── Health ──────────────────────────────────────────────────────────────
    [Header("Stats")] [SerializeField] private int maxHp = 30;
    public int MaxHp => State.MaxHp;
    public int CurrentHp => State.Hp;

    // ─── Movement animation ──────────────────────────────────────────────────
    [Header("Movement")] [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private float postMovePause = 0.2f;

    /// <summary>True from move start until the post-move pause ends.</summary>
    public bool IsMoving { get; private set; }

    // ─── Abilities ────────────────────────────────────────────────────────────
    [Header("Abilities")] [SerializeField] private List<AbilityData> abilities = new List<AbilityData>();

    /// <summary>Read-only view of the unit's ability list.</summary>
    public IReadOnlyList<AbilityData> Abilities => State.Abilities;

    /// <summary>True from ability start until its effects (and later its animation) finish.</summary>
    public bool IsUsingAbility { get; private set; }

    /// <summary>True while the unit is moving or using an ability; no new action may start.</summary>
    public bool IsActing => IsMoving || IsUsingAbility;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnDestroy()
    {
        if (State == null) return;
        State.HealthChanged -= HandleHealthChanged;
        State.Died -= HandleDied;
    }

    // ─── Setup ───────────────────────────────────────────────────────────────
    /// <summary>Creates the unit's state from its configured stats and links it to the grid. Call before placing it.</summary>
    public void Init(PlayerID ownerID, Color ownerColor, GridManager grid)
    {
        _grid = grid;
        State = new UnitState(ownerID, maxHp, abilities.ToArray());
        State.HealthChanged += HandleHealthChanged;
        State.Died += HandleDied;
        _grid.RegisterUnitView(this);
        ApplyOwnerColor(ownerColor);
    }

    private void ApplyOwnerColor(Color color) // TODO: Remove when using a proper material system for units
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = color;
            unitColor = color;
        }
    }

    /// <summary>Sets the unit's number and shows it as a label on top of the sprite.</summary>
    public void SetNumber(int number)
    {
        unitID = number;
        var label = new GameObject("NumberLabel").AddComponent<TextMeshPro>();
        label.transform.SetParent(transform, false);
        label.rectTransform.sizeDelta = Vector2.one;
        label.text = number.ToString();
        label.fontSize = 6;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        if (_spriteRenderer != null)
        {
            label.sortingLayerID = _spriteRenderer.sortingLayerID;
            label.sortingOrder = _spriteRenderer.sortingOrder + 1;
        }
    }

    // ─── Movement (System) ───────────────────────────────────────────────────
    /// <summary>Snaps the view onto a tile after its state was placed there (spawn / teleport).</summary>
    public void PlaceOnTile(Tile targetTile)
    {
        transform.position = targetTile.transform.position;
    }

    // ─── Movement (Gameplay) ─────────────────────────────────────────────────
    /// <summary>
    /// Animates the view to a tile after <see cref="Match.TryMove"/> has moved its state there,
    /// then after a short pause invokes <paramref name="onComplete"/>.
    /// </summary>
    public void MoveToTile(Tile targetTile, Action onComplete = null)
    {
        StartCoroutine(AnimateMove(targetTile.transform.position, onComplete));
    }

    private IEnumerator AnimateMove(Vector3 target, Action onComplete)
    {
        IsMoving = true;
        Vector3 start = transform.position;
        for (float t = 0f; t < moveDuration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / moveDuration));
            yield return null;
        }

        transform.position = target;
        yield return new WaitForSeconds(postMovePause);
        IsMoving = false;
        onComplete?.Invoke();
    }

    // ─── Abilities (Gameplay) ────────────────────────────────────────────────
    /// <summary>
    /// Plays the ability after <see cref="Match.TryUseAbility"/> has applied it, then invokes <paramref name="onComplete"/>.
    /// </summary>
    public void PlayAbility(Action onComplete = null)
    {
        // shortcut: no ability animation yet, so this finishes in the same frame; turn it into a coroutine like AnimateMove when animations exist
        IsUsingAbility = true;
        IsUsingAbility = false;
        onComplete?.Invoke();
    }

    // ─── Health ──────────────────────────────────────────────────────────────
    private void HandleHealthChanged(UnitState state) => Debug.Log($"{name} HP: {CurrentHp}/{MaxHp}");

    private void HandleDied(UnitState state)
    {
        Debug.Log($"{name} died.");
        // Match removes the state from the board and turn order; this only tears down the view
        _grid.UnregisterUnitView(this);
        Destroy(gameObject);
    }
}