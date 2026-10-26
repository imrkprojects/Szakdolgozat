using UnityEngine;

/// <summary>Moves and zooms the camera in response to grid and unit selection events.</summary>
public class CameraController : MonoBehaviour
{
    private Vector3 _targetPosition;
    private Vector3 _gridCenter;
    private Camera _camera;

    [SerializeField] private float cameraSmoothing = 6f;
    [Header("Zoom")] [SerializeField] private float overviewSize = 8f; // Zoomed out — no selection
    [SerializeField] private float focusedSize = 5f; // Zoomed in — unit selected

    private float _targetSize;
    private Unit _followUnit;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _targetPosition = transform.position;
        _targetSize = _camera.orthographicSize;
    }

    private void OnEnable()
    {
        GridManager.OnGridGenerated += HandleGridGenerated;
        SelectionManager.OnUnitSelected += HandleUnitSelected;
        SelectionManager.OnUnitDeselected += HandleUnitDeselected;
        SelectionManager.OnSelectedUnitMoved += HandleSelectedUnitMoved;
    }

    private void OnDisable()
    {
        GridManager.OnGridGenerated -= HandleGridGenerated;
        SelectionManager.OnUnitSelected -= HandleUnitSelected;
        SelectionManager.OnUnitDeselected -= HandleUnitDeselected;
        SelectionManager.OnSelectedUnitMoved -= HandleSelectedUnitMoved;
    }

    /// <summary>Initial camera position — snaps instantly to grid center, zoomed out.</summary>
    private void HandleGridGenerated(Vector3 gridCenter)
    {
        _gridCenter = gridCenter;
        _targetPosition = new Vector3(gridCenter.x, gridCenter.y, -10f);
        transform.position = _targetPosition;
        _targetSize = overviewSize;
        _camera.orthographicSize = overviewSize;
    }

    /// <summary>Smoothly pans to the selected unit and zooms in.</summary>
    private void HandleUnitSelected(Unit unit)
    {
        Vector3 unitPos = unit.CurrentTile.transform.position;
        _targetPosition = new Vector3(unitPos.x, unitPos.y, -10f);
        _targetSize = focusedSize;
    }

    /// <summary>Follows the selected unit's transform while its movement animation plays.</summary>
    private void HandleSelectedUnitMoved(Unit unit)
    {
        _followUnit = unit; // Update() tracks its transform while it animates
    }

    /// <summary>Smoothly pans back to grid center on deselect and zooms out.</summary>
    private void HandleUnitDeselected()
    {
        _targetPosition = new Vector3(_gridCenter.x, _gridCenter.y, -10f);
        _targetSize = overviewSize;
    }
    
    /// <summary>Updates camera for following animated unit.</summary>
    private void Update()
    {
        if (_followUnit != null && _followUnit.IsMoving)
            _targetPosition = new Vector3(_followUnit.transform.position.x, _followUnit.transform.position.y, -10f);

        transform.position = Vector3.Lerp(transform.position, _targetPosition, cameraSmoothing * Time.deltaTime);
        _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _targetSize, cameraSmoothing * Time.deltaTime);
    }
}
