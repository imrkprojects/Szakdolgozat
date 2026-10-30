using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Abstract base that defines which tiles can be targeted by an ability.
/// Subclasses implement different shapes: diamond, cross, line, etc.
/// </summary>
public abstract class TargetingRule : ScriptableObject
{
    /// <summary>
    /// Returns all cells the caster can point an ability at.
    /// AbilityState only accepts clicks on tiles holding an enemy unit.
    /// </summary>
    public abstract List<Vector2Int> GetTargetablePositions(Board board, UnitState caster);
}