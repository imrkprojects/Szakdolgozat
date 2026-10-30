using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Targets all tiles within a given Manhattan-distance range of the caster
/// forming a diamond (rhombus) shape on the grid.
/// Create via: right-click → Create → Game → Ability Targeting → Diamond
/// </summary>
[CreateAssetMenu(fileName = "DiamondTargeting", menuName = "Game/Ability Targeting/Diamond")]
public class DiamondTargetingRule : TargetingRule
{
    [Tooltip("Maximum Manhattan distance from the caster tile.")] [SerializeField]
    private int range = 2;

    [Tooltip("If true the caster's own tile is excluded from the targetable set.")] [SerializeField]
    private bool excludeSelf = true;

    public override List<Vector2Int> GetTargetablePositions(Board board, UnitState caster)
    {
        return board.GetInRange(caster.Position, range, excludeSelf);
    }
}