using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Unit : MonoBehaviour
{
    public int unitID;

    // ─── Appearance ──────────────────────────────────────────────────────────
    [SerializeField] private Color unitColor;
    private SpriteRenderer _spriteRenderer;

    // ─── Ownership ───────────────────────────────────────────────────────────
    public PlayerID OwnerID { get; private set; } = PlayerID.None;

    // ─── Position ────────────────────────────────────────────────────────────
    public Tile CurrentTile { get; private set; }

    // ─── Health ──────────────────────────────────────────────────────────────
    [Header("Stats")] [SerializeField] private int maxHp = 30;
    public int MaxHp => maxHp;
    public int CurrentHp { get; private set; }

    // ─── Movement animation ──────────────────────────────────────────────────
    [Header("Movement")] [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private float postMovePause = 0.2f;

    /// <summary>True from move start until the post-move pause ends.</summary>
    public bool IsMoving { get; private set; }

    // ─── Abilities ────────────────────────────────────────────────────────────
    [Header("Abilities")] [SerializeField] private List<AbilityData> abilities = new List<AbilityData>();

    /// <summary>Read-only view of the unit's ability list.</summary>
    public IReadOnlyList<AbilityData> Abilities => abilities;

    // ─── Events ──────────────────────────────────────────────────────────────
    public static event Action<Unit> OnUnitPlaced;
    public static event Action<Unit> OnUnitMoved;
    public static event Action<Unit> OnUnitDied;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        CurrentHp = maxHp;
    }

    // ─── Ownership ───────────────────────────────────────────────────────────
    public void SetOwner(PlayerID ownerID)
    {
        OwnerID = ownerID;
        ApplyOwnerColor();
    }

    private void ApplyOwnerColor() // TODO: Remove when using a proper material system for units
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer != null && PlayerManager.Instance != null)
        {
            Player owner = PlayerManager.Instance.GetPlayer(OwnerID);
            if (owner != null)
            {
                _spriteRenderer.color = owner.Color;
                unitColor = owner.Color;
            }
        }
    }

    // ─── Movement (System) ───────────────────────────────────────────────────
    /// <summary>Teleport / spawn / system placement — does not fire OnUnitMoved.</summary>
    public void PlaceOnTile(Tile targetTile, bool snap = true)
    {
        if (CurrentTile != null) CurrentTile.OccupyingUnit = null;
        CurrentTile = targetTile;
        CurrentTile.OccupyingUnit = this;
        if (snap) transform.position = targetTile.transform.position;
        OnUnitPlaced?.Invoke(this);
    }

    // ─── Movement (Gameplay) ─────────────────────────────────────────────────
    /// <summary>
    /// Player-initiated move. Logical position changes immediately (OnUnitMoved fires),
    /// the transform animates, then after a short pause <paramref name="onComplete"/> is invoked.
    /// </summary>
    public void MoveToTile(Tile targetTile, Action onComplete = null)
    {
        PlaceOnTile(targetTile, snap: false);
        OnUnitMoved?.Invoke(this);
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

    public bool CanMoveTo(Tile targetTile)
    {
        if (targetTile.OccupyingUnit != null) return false;
        int deltaX = Mathf.Abs(CurrentTile.Coords.x - targetTile.Coords.x);
        int deltaY = Mathf.Abs(CurrentTile.Coords.y - targetTile.Coords.y);
        return (deltaX + deltaY) == 1;
    }

    // ─── Health Modification ──────────────────────────────────────────────────────────────
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        Debug.Log($"{name} took {amount} damage — HP: {CurrentHp}/{MaxHp}");
        if (CurrentHp <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        Debug.Log($"{name} healed {amount} — HP: {CurrentHp}/{MaxHp}");
    }

    private void Die()
    {
        Debug.Log($"{name} died.");
        TurnManager.Instance.UnregisterUnit(this);
        OnUnitDied?.Invoke(this);
        if (CurrentTile != null) CurrentTile.OccupyingUnit = null;
        Destroy(gameObject);
    }
}