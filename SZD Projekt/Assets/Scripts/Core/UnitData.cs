using UnityEngine;

/// <summary>
/// Config of one unit type (stats and abilities), shared by every unit of that type.
/// Create instances via: right-click in Project → Create → Game → Unit
/// </summary>
[CreateAssetMenu(fileName = "NewUnit", menuName = "Game/Unit")]
public class UnitData : ScriptableObject
{
    [Header("Identity")] public string unitName = "Unit";

    [Tooltip("Optional; the prefab's sprite is kept when empty.")]
    public Sprite sprite;

    [Header("Stats")] [Min(1)] public int maxHp = 30;

    [Tooltip("Orthogonal steps through empty cells per move.")] [Min(0)]
    public int moveRange = 1;

    [Header("Abilities")] public AbilityData[] abilities = new AbilityData[0];

    /// <summary>Creates the runtime state of a new unit of this type.</summary>
    public UnitState CreateState(PlayerID owner) =>
        // Copied so editing the asset during a match can't change units already created
        new UnitState(owner, maxHp, abilities == null ? null : (AbilityData[])abilities.Clone(), moveRange);
}