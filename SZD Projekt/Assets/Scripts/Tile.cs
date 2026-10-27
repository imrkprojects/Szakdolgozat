using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Visual state used to choose a tile's highlight color.</summary>
public enum TileHighlightState
{
    None, // Default grey
    Reachable, // Valid move target — green
    Selected, // Unit is standing here — yellow
    AbilityRange, // Tile is within ability range — orange
    AbilityTarget // Tile is the chosen ability target — red/purple
}

/// <summary>Stores a grid tile's coordinates, occupant, and pointer/highlight presentation.</summary>
public class Tile : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color baseColor = new Color(0.2f, 0.2f, 0.2f);
    [SerializeField] private Color hoverColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color reachableColor = new Color(0.2f, 0.6f, 0.2f);
    [SerializeField] private Color selectedColor = new Color(0.8f, 0.75f, 0.1f);
    [SerializeField] private Color abilityRangeColor = new Color(0.9f, 0.5f, 0.1f);
    [SerializeField] private Color abilityTargetColor = new Color(0.8f, 0.1f, 0.1f);

    private SpriteRenderer _spriteRenderer;
    private TileHighlightState _currentHighlight = TileHighlightState.None;
    private bool _isHovered;

    /// <summary>Grid coordinates assigned when the tile is initialized.</summary>
    public Vector2Int Coords { get; private set; }

    /// <summary>The unit on this tile, or null when the tile is empty.</summary>
    public Unit OccupyingUnit { get; set; }

    /// <summary>Sets the tile's grid coordinates and initializes its renderer.</summary>
    public void Init(int x, int y)
    {
        Coords = new Vector2Int(x, y);
        _spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyColor();
    }

    private void OnMouseEnter()
    {
        _isHovered = true;
        ApplyColor();
    }

    private void OnMouseExit()
    {
        _isHovered = false;
        ApplyColor();
    }

    private void OnMouseDown()
    {
        // OnMouseDown ignores UI, so a click on a button over the grid would also hit this tile
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        GridManager.Instance.OnTileClicked(this);
    }

    /// <summary>Changes the tile highlight and immediately refreshes its color.</summary>
    public void SetHighlight(TileHighlightState state)
    {
        _currentHighlight = state;
        ApplyColor();
    }

    private void ApplyColor()
    {
        switch (_currentHighlight)
        {
            case TileHighlightState.Selected:
                _spriteRenderer.color = selectedColor;
                return;

            case TileHighlightState.Reachable:
                _spriteRenderer.color = _isHovered
                    ? Color.Lerp(reachableColor, Color.white, 0.2f)
                    : reachableColor;
                return;

            case TileHighlightState.AbilityRange:
                _spriteRenderer.color = _isHovered
                    ? Color.Lerp(abilityRangeColor, Color.white, 0.2f)
                    : abilityRangeColor;
                return;

            case TileHighlightState.AbilityTarget:
                _spriteRenderer.color = abilityTargetColor;
                return;

            default: // None
                _spriteRenderer.color = _isHovered ? hoverColor : baseColor;
                return;
        }
    }
}