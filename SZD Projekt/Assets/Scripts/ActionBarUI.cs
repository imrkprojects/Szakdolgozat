using UnityEngine;
using UnityEngine.UI;

/// <summary>Move/ability buttons for the selected unit; shown only while a unit is selected.</summary>
/// <remarks>Attach this to an always-active object so it can subscribe before the panel is shown.</remarks>
public class ActionBarUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;        // ActionBarPanel
    [SerializeField] private Button abilityButton;    // AbilityButton

    private Unit _unit;

    private void Awake()
    {
        panel.SetActive(false);
    }

    private void OnEnable()
    {
        SelectionManager.OnUnitSelected += HandleUnitSelected;
        SelectionManager.OnUnitDeselected += HandleUnitDeselected;
    }

    private void OnDisable()
    {
        SelectionManager.OnUnitSelected -= HandleUnitSelected;
        SelectionManager.OnUnitDeselected -= HandleUnitDeselected;
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
    public void OnMoveClicked() => SelectionManager.Instance.EnterMoveMode();

    /// <summary>Switches the selected unit to targeting with its first ability. Wire this to the Ability button.</summary>
    public void OnAbilityClicked()
    {
        if (_unit != null && _unit.Abilities.Count > 0)
            SelectionManager.Instance.EnterAbilityMode(_unit.Abilities[0]);
    }
}
