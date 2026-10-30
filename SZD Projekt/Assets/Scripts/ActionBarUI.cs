using UnityEngine;
using UnityEngine.UI;

/// <summary>Move/ability buttons for the selected unit; shown only while a unit is selected.</summary>
public class ActionBarUI : MonoBehaviour
{
    [SerializeField] private GameObject panel; // ActionBarPanel
    [SerializeField] private Button abilityButton; // AbilityButton

    private SelectionManager _selection;
    private Unit _unit;

    private void Awake()
    {
        panel.SetActive(false);
    }

    /// <summary>Links the bar to the selection it shows and controls.</summary>
    public void Init(SelectionManager selection)
    {
        _selection = selection;
        _selection.OnUnitSelected += HandleUnitSelected;
        _selection.OnUnitDeselected += HandleUnitDeselected;
    }

    private void OnDestroy()
    {
        if (_selection == null) return;
        _selection.OnUnitSelected -= HandleUnitSelected;
        _selection.OnUnitDeselected -= HandleUnitDeselected;
    }

    private void HandleUnitSelected(Unit unit)
    {
        _unit = unit;
        abilityButton.interactable = unit.Abilities.Count > 0;
        panel.SetActive(true);
    }

    private void HandleUnitDeselected()
    {
        _unit = null;
        panel.SetActive(false);
    }

    /// <summary>Switches the selected unit to movement mode. Wire this to the Move button.</summary>
    public void OnMoveClicked() => _selection.EnterMoveMode();

    /// <summary>Switches the selected unit to targeting with its first ability. Wire this to the Ability button.</summary>
    public void OnAbilityClicked()
    {
        if (_unit != null && _unit.Abilities.Count > 0)
            _selection.EnterAbilityMode(_unit.Abilities[0]);
    }
}