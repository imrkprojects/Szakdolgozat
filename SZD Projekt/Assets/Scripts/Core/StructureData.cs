using UnityEngine;

/// <summary>
/// Config of one structure type (wall, bush, …) that can stand on a cell. Walls and bushes differ only in the flags.
/// Create instances via: right-click in Project → Create → Game → Structure
/// </summary>
[CreateAssetMenu(fileName = "NewStructure", menuName = "Game/Structure")]
public class StructureData : ScriptableObject
{
    [Header("Identity")] public string structureName = "Structure";
    public Sprite sprite;

    [Header("Tags")] [Tooltip("Units can't enter the cell (a wall). False for a bush.")]
    public bool blocksMovement = true;

    [Tooltip("Stored for the future vision system; no rule reads it yet.")]
    public bool blocksVision = true;

    [Tooltip("Can be shot through. Stored for future projectiles; no rule reads it yet.")]
    public bool piercable;

    [Header("Durability")] [Tooltip("Damage abilities can target it and destroy it.")]
    public bool destructible;

    [Min(1)] public int maxHp = 10;

    /// <summary>Creates the runtime state of a new structure of this type.</summary>
    public StructureState CreateState() => new StructureState(this);
}
