using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Deals a fixed amount of damage to every unit on the target tiles
/// (except the caster, so self-damage is never accidental).
/// Create via: right-click → Create → Game → Ability Effects → Damage
/// </summary>
[CreateAssetMenu(fileName = "DamageEffect", menuName = "Game/Ability Effects/Damage")]
public class DamageEffect : AbilityEffect
{
    [SerializeField] private int damage = 10;

    public override void Apply(Unit caster, List<Tile> targetTiles)
    {
        foreach (Tile tile in targetTiles)
        {
            Unit target = tile.OccupyingUnit;
            if (target != null && target != caster)
            {
                target.TakeDamage(damage);
            }
        }
    }
}
