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

    public override void Apply(Unit caster, List<Tile> targetTiles)
    {
        foreach (Tile tile in targetTiles)
        {
            Unit target = tile.OccupyingUnit;
            if (target != null && target.OwnerID != caster.OwnerID)
            {
                target.TakeDamage(damage);
            }
        }
    }
}