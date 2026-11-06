using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A complete ability composed of a targeting rule and one or more ordered effects.
/// Create instances via: right-click in Project → Create → Game → Ability
/// </summary>
[CreateAssetMenu(fileName = "NewAbility", menuName = "Game/Ability")]
public class AbilityData : ScriptableObject
{
    [Header("Identity")] public string abilityName = "Unnamed Ability";
    public Sprite icon;

    [Header("Targeting")] [Tooltip("Defines the shape and range of tiles the player can aim the ability at.")]
    public TargetingRule targetingRule;

    [Tooltip("Which cells in range the ability may actually be used on.")]
    public TargetFilter targetFilter = TargetFilter.Enemy;

    [Header("Cooldown")]
    [Tooltip("After use, the unit can't use this ability for this many of its own turns. 0 = always ready.")]
    [Min(0)]
    public int cooldown;

    [Header("Effects — applied in order")] [Tooltip("Chain multiple effects: damage first, then knockback, etc.")]
    public AbilityEffect[] effects;

    /// <summary>Returns the cells the caster can target with this ability.</summary>
    public List<Vector2Int> GetTargetablePositions(Board board, UnitState caster)
    {
        if (targetingRule == null)
        {
            Debug.LogWarning($"Ability '{abilityName}' has no TargetingRule assigned.");
            return new List<Vector2Int>();
        }

        return targetingRule.GetTargetablePositions(board, caster);
    }

    /// <summary>True if what stands on <paramref name="cell"/> matches <see cref="targetFilter"/>. Range is checked separately.</summary>
    public bool IsValidTarget(Board board, UnitState caster, Vector2Int cell)
    {
        UnitState occupant = board.GetUnitAt(cell);
        switch (targetFilter)
        {
            case TargetFilter.Enemy: return occupant != null && occupant.Owner != caster.Owner;
            case TargetFilter.Ally: return occupant != null && occupant.Owner == caster.Owner;
            case TargetFilter.Self: return occupant == caster;
            case TargetFilter.AnyUnit: return occupant != null;
            case TargetFilter.EmptyCell: return occupant == null;
            default: return false;
        }
    }

    /// <summary>Executes all effects against the given target cells in order.</summary>
    public void Execute(Match match, UnitState caster, IReadOnlyList<Vector2Int> targets)
    {
        foreach (AbilityEffect effect in effects)
        {
            if (effect != null)
                effect.Apply(match, caster, targets);
        }
    }
}