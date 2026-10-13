using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Vector3 _targetPosition;
    private Vector3 _gridCenter;

    [SerializeField] private float cameraSmoothing = 5f;
    
    private void Awake()
    {
        _targetPosition = transform.position;
    }
    
    private void OnEnable()
    {
        GridManager.OnGridGenerated             += HandleGridGenerated;
        SelectionManager.OnUnitSelected         += HandleUnitSelected;
        SelectionManager.OnUnitDeselected       += HandleUnitDeselected;
        SelectionManager.OnSelectedUnitMoved    += HandleSelectedUnitMoved;
    }
    
    private void OnDisable()
    {
        GridManager.OnGridGenerated             -= HandleGridGenerated;
        SelectionManager.OnUnitSelected         -= HandleUnitSelected;
        SelectionManager.OnUnitDeselected       -= HandleUnitDeselected;
        SelectionManager.OnSelectedUnitMoved    -= HandleSelectedUnitMoved;
    }
    
    // Initial camera pos — snaps instantly to grid center
    private void HandleGridGenerated(Vector3 gridCenter)
    {
        _gridCenter = gridCenter;
        _targetPosition = new Vector3(gridCenter.x, gridCenter.y, -10f);
        transform.position = _targetPosition; 
    }
    
    // Smoothly pan to unit on selection
    private void HandleUnitSelected(Unit unit)
    {
        Vector3 unitPos = unit.CurrentTile.transform.position;
        _targetPosition = new Vector3(unitPos.x, unitPos.y, -10f);
    }
    
    // Smoothly pan to unit's new tile after it moves (Only when selected)
    private void HandleSelectedUnitMoved(Unit unit)
    {
        Vector3 unitPos = unit.CurrentTile.transform.position;
        _targetPosition = new Vector3(unitPos.x, unitPos.y, -10f);
    }
    
    // Smoothly pan back to grid center on deselect
    private void HandleUnitDeselected()
    {
        _targetPosition = new Vector3(_gridCenter.x, _gridCenter.y, -10f);
    }
    
    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _targetPosition, cameraSmoothing * Time.deltaTime);
    }
}