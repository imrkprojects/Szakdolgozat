using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A complete ability composed of a targeting rule and one or more ordered effects.
/// Create instances via: right-click in Project → Create → Game → Ability
/// </summary>
[CreateAssetMenu(fileName = "NewAbility", menuName = "Game/Ability")]
public class AbilityData : ScriptableObject
{
    [Header("Identity")]
    public string abilityName = "Unnamed Ability";
    public Sprite icon;

    [Header("Targeting")]
    [Tooltip("Defines the shape and range of tiles the player can aim the ability at.")]
    public TargetingRule targetingRule;

    [Header("Effects — applied in order")]
    [Tooltip("Chain multiple effects: damage first, then knockback, etc.")]
    public AbilityEffect[] effects;

    /// <summary>Returns tiles the caster can target with this ability.</summary>
    public List<Tile> GetTargetableTiles(Unit caster)
    {
        if (targetingRule == null)
        {
            Debug.LogWarning($"Ability '{abilityName}' has no TargetingRule assigned.");
            return new List<Tile>();
        }
        return targetingRule.GetTargetableTiles(caster);
    }

    /// <summary>Executes all effects against the given target tiles in order.</summary>
    public void Execute(Unit caster, List<Tile> targetTiles)
    {
        foreach (AbilityEffect effect in effects)
        {
            if (effect != null)
                effect.Apply(caster, targetTiles);
        }
    }
}
