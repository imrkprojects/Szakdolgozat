using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Abstract base that defines which tiles can be targeted by an ability.
/// Subclasses implement different shapes: diamond, cross, line, etc.
/// </summary>
public abstract class TargetingRule : ScriptableObject
{
    /// <summary>
    /// Returns all tiles the caster can point an ability at.
    /// Filtering (enemy-only, occupied-only, etc.) is handled separately in AbilityData.
    /// </summary>
    public abstract List<Tile> GetTargetableTiles(Unit caster);
}
