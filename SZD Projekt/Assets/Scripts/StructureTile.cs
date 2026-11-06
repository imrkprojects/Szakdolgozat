using UnityEngine;

/// <summary>
/// Tile that paints a structure onto the structure Tilemap; <see cref="GridManager"/> reads it back into the board.
/// Create via: right-click → Create → Game → Structure Tile
/// </summary>
[CreateAssetMenu(fileName = "NewStructureTile", menuName = "Game/Structure Tile")]
public class StructureTile : UnityEngine.Tilemaps.TileBase
{
    public StructureData data;

    public override void GetTileData(Vector3Int position, UnityEngine.Tilemaps.ITilemap tilemap,
        ref UnityEngine.Tilemaps.TileData tileData)
    {
        tileData.sprite = data != null ? data.sprite : null;
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
    }
}
