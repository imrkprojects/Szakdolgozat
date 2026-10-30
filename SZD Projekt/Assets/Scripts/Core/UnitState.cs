using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Game state of one unit: owner, health and board position. Has no scene presence.</summary>
public class UnitState
{
    public PlayerID Owner { get; }
    public int MaxHp { get; }
    public int Hp { get; private set; }
    public bool IsAlive => Hp > 0;
    public IReadOnlyList<AbilityData> Abilities { get; }

    /// <summary>Board cell of the unit. Only <see cref="Board"/> changes it.</summary>
    public Vector2Int Position { get; internal set; }

    /// <summary>Raised after every change to <see cref="Hp"/>.</summary>
    public event Action<UnitState> HealthChanged;

    /// <summary>Raised once, when <see cref="Hp"/> reaches 0.</summary>
    public event Action<UnitState> Died;

    public UnitState(PlayerID owner, int maxHp, IReadOnlyList<AbilityData> abilities = null)
    {
        if (maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(maxHp), "Max HP must be positive.");
        Owner = owner;
        MaxHp = maxHp;
        Hp = maxHp;
        Abilities = abilities ?? Array.Empty<AbilityData>();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || !IsAlive) return;
        Hp = Mathf.Max(0, Hp - amount);
        HealthChanged?.Invoke(this);
        if (!IsAlive) Died?.Invoke(this);
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || !IsAlive) return;
        Hp = Mathf.Min(MaxHp, Hp + amount);
        HealthChanged?.Invoke(this);
    }
}