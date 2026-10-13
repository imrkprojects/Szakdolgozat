using UnityEngine;
using System;

public class Unit : MonoBehaviour
{
    public int unitID;
    
    public Tile CurrentTile { get; private set; }
    public static event Action<Unit> OnUnitPlaced;
    public static event Action<Unit> OnUnitMoved;
    
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