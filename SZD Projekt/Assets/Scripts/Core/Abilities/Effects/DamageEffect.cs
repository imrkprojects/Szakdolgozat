using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Deals a fixed amount of damage to every enemy unit on the target tiles
/// (the caster and its allies are never hit).
/// Create via: right-click → Create → Game → Ability Effects → Damage
/// </summary>
[CreateAssetMenu(fileName = "DamageEffect", menuName = "Game/Ability Effects/Damage")]
public class DamageEffect : AbilityEffect
{
    [SerializeField] private int damage = 10;

    public override void Apply(Match match, UnitState caster, IReadOnlyList<Vector2Int> targets)
    {
        foreach (Vector2Int pos in targets)
        {
            UnitState target = match.Board.GetUnitAt(pos);
            if (target != null && target.Owner != caster.Owner)
            {
                Debug.Log($"Unit at {pos} takes {damage} damage");
                target.TakeDamage(damage);
            }
        }
    }
}