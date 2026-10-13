using UnityEngine;

public enum TileHighlightState
{
    None,       // Default grey
    Reachable,  // Green
    Selected    // Yellow
}

public class Tile : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color baseColor    = new Color(0.2f, 0.2f, 0.2f);
    [SerializeField] private Color hoverColor   = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color reachableColor = new Color(0.2f, 0.6f, 0.2f);
    [SerializeField] private Color selectedColor  = new Color(0.8f, 0.75f, 0.1f);

    private SpriteRenderer _spriteRenderer;
    private TileHighlightState _currentHighlight = TileHighlightState.None;
    private bool _isHovered;
    
    public Vector2Int Coords { get; private set; }
    public Unit OccupyingUnit { get; set; }

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
        GridManager.Instance.OnTileClicked(this);
    }

    public void SetHighlight(TileHighlightState state)
    {
        _currentHighlight = state;
        ApplyColor();
    }
    
    private void ApplyColor()
    {
        if (_currentHighlight == TileHighlightState.Selected)
        {
            _spriteRenderer.color = selectedColor;
            return;
        }
        if (_currentHighlight == TileHighlightState.Reachable)
        {
            _spriteRenderer.color = _isHovered
                ? Color.Lerp(reachableColor, Color.white, 0.2f) // Slightly brighter on hover
                : reachableColor;
            return;
        }
        // Default state
        _spriteRenderer.color = _isHovered ? hoverColor : baseColor;
    }
}