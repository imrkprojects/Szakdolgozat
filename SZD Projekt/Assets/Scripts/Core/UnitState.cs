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

    /// <summary>How many orthogonal steps through walkable cells the unit may take in one move.</summary>
    public int MoveRange { get; }

    /// <summary>Board cell of the unit. Only <see cref="Board"/> changes it.</summary>
    public Vector2Int Position { get; internal set; }

    /// <summary>Raised after every change to <see cref="Hp"/>.</summary>
    public event Action<UnitState> HealthChanged;

    /// <summary>Raised once, when <see cref="Hp"/> reaches 0.</summary>
    public event Action<UnitState> Died;

    public UnitState(PlayerID owner, int maxHp, IReadOnlyList<AbilityData> abilities = null, int moveRange = 1)
    {
        if (maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(maxHp), "Max HP must be positive.");
        if (moveRange < 0) throw new ArgumentOutOfRangeException(nameof(moveRange), "Move range can't be negative.");
        Owner = owner;
        MoveRange = moveRange;
        MaxHp = maxHp;
        Hp = maxHp;
        Abilities = abilities ?? Array.Empty<AbilityData>();
    }

    // Remaining own turns each ability stays unusable; abilities not in the map are ready
    private readonly Dictionary<AbilityData, int> _cooldowns = new Dictionary<AbilityData, int>();

    /// <summary>How many more of this unit's own turns the ability stays unusable (0 = ready).</summary>
    public int GetCooldown(AbilityData ability) =>
        ability != null && _cooldowns.TryGetValue(ability, out int turns) ? turns : 0;

    /// <summary>
    /// Puts the ability on cooldown after use. One extra turn is added because the end of the turn
    /// it was used in ticks too, so it is unusable for exactly the next <see cref="AbilityData.cooldown"/> own turns.
    /// </summary>
    internal void StartCooldown(AbilityData ability)
    {
        if (ability.cooldown > 0) _cooldowns[ability] = ability.cooldown + 1;
    }

    /// <summary>Counts every cooldown down by one; called when this unit's turn ends.</summary>
    internal void TickCooldowns()
    {
        foreach (AbilityData ability in new List<AbilityData>(_cooldowns.Keys))
        {
            if (--_cooldowns[ability] <= 0) _cooldowns.Remove(ability);
        }
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