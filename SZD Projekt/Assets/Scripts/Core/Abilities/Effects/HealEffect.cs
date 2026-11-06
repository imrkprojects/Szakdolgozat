using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Restores health to every friendly unit (the caster included) on the target cells; enemies are never healed.
/// Create via: right-click → Create → Game → Ability Effects → Heal
/// </summary>
[CreateAssetMenu(fileName = "HealEffect", menuName = "Game/Ability Effects/Heal")]
public class HealEffect : AbilityEffect
{
    [SerializeField] private int amount = 8;

    public override void Apply(Match match, UnitState caster, IReadOnlyList<Vector2Int> targets)
    {
        foreach (Vector2Int pos in targets)
        {
            UnitState target = match.Board.GetUnitAt(pos);
            if (target != null && target.Owner == caster.Owner) target.Heal(amount);
        }
    }
}
