using System;
using UnityEngine;

/// <summary>Game state of one structure on the board: its type, position and health. Has no scene presence.</summary>
public class StructureState
{
    public StructureData Data { get; }
    public int Hp { get; private set; }
    public bool IsDestroyed => Hp <= 0;

    /// <summary>Board cell of the structure. Only <see cref="Board"/> changes it.</summary>
    public Vector2Int Position { get; internal set; }

    /// <summary>Raised after every change to <see cref="Hp"/>.</summary>
    public event Action<StructureState> HealthChanged;

    /// <summary>Raised once, when <see cref="Hp"/> reaches 0.</summary>
    public event Action<StructureState> Destroyed;

    public StructureState(StructureData data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(data), "Max HP must be positive.");
        Data = data;
        Hp = data.maxHp;
    }

    /// <summary>Lowers HP; ignored for indestructible or destroyed structures and non-positive amounts.</summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0 || !Data.destructible || IsDestroyed) return;
        Hp = Mathf.Max(0, Hp - amount);
        HealthChanged?.Invoke(this);
        if (IsDestroyed) Destroyed?.Invoke(this);
    }
}
