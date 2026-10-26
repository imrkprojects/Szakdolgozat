using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Abstract base for every ability effect (damage, heal, push, etc.).
/// Create concrete subclasses as ScriptableObjects via the Create menu.
/// </summary>
public abstract class AbilityEffect : ScriptableObject
{
    /// <summary>
    /// Apply the effect to all resolved target tiles.
    /// The caster is provided so effects can use origin info (e.g. knockback direction).
    /// </summary>
    public abstract void Apply(Unit caster, List<Tile> targetTiles);
}
