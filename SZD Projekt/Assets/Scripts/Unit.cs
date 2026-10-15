using UnityEngine;
using System;

public class Unit : MonoBehaviour
{
    public int unitID;

    [SerializeField] private Color unitColor;
    public PlayerID OwnerID { get; private set; } = PlayerID.None;
    
    private SpriteRenderer _spriteRenderer;
    public Tile CurrentTile { get; private set; }
    public static event Action<Unit> OnUnitPlaced;
    public static event Action<Unit> OnUnitMoved;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetOwner(PlayerID ownerID)
    {
        OwnerID = ownerID;
        ApplyOwnerColor();
    }

    private void ApplyOwnerColor()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

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
    
    // System movement (Spawn, Despawn, Teleport...)
    public void PlaceOnTile(Tile targetTile)
    {
        if (CurrentTile != null) CurrentTile.OccupyingUnit = null;
        CurrentTile = targetTile;
        CurrentTile.OccupyingUnit = this;
        transform.position = targetTile.transform.position;
        OnUnitPlaced?.Invoke(this);
    }
    
    // Gameplay movement
    public void MoveToTile(Tile targetTile)
    {
        PlaceOnTile(targetTile);
        OnUnitMoved?.Invoke(this);
    }
    
    public bool CanMoveTo(Tile targetTile)
    {
        if (targetTile.OccupyingUnit != null) return false;

        // Check if target is adjacent (Manhattan distance == 1: Up, Down, Left, Right)
        int deltaX = Mathf.Abs(CurrentTile.Coords.x - targetTile.Coords.x);
        int deltaY = Mathf.Abs(CurrentTile.Coords.y - targetTile.Coords.y);

        return (deltaX + deltaY) == 1;
    }
}