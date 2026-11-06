using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Move/ability buttons for the selected unit; shown only while a unit is selected.</summary>
public class ActionBarUI : MonoBehaviour
{
    [SerializeField] private GameObject panel; // ActionBarPanel
    [SerializeField] private Button[] abilityButtons; // AbilityButton1..4, each calling OnAbilityClicked(its index)

    private SelectionManager _selection;
    private Match _match;
    private Unit _unit;

    private void Awake()
    {
        panel.SetActive(false);
    }

    /// <summary>Links the bar to the selection it shows and controls, and the match that decides which abilities are usable.</summary>
    public void Init(SelectionManager selection, Match match)
    {
        _selection = selection;
        _match = match;
        _selection.OnUnitSelected += HandleUnitSelected;
        _selection.OnUnitDeselected += HandleUnitDeselected;
    }

    private void OnDestroy()
    {
        if (_selection == null) return;
        _selection.OnUnitSelected -= HandleUnitSelected;
        _selection.OnUnitDeselected -= HandleUnitDeselected;
    }

    // Selection is renewed on every turn change, so this also refreshes the buttons after each action
    private void HandleUnitSelected(Unit unit)
    {
        _unit = unit;
        for (int i = 0; i < abilityButtons.Length; i++)
        {
            bool used = i < unit.Abilities.Count;
            abilityButtons[i].gameObject.SetActive(used);
            if (!used) continue;
            AbilityData ability = unit.Abilities[i];
            abilityButtons[i].interactable = _match.CanUseAbility(unit.State, ability);
            int cooldown = unit.State.GetCooldown(ability);
            abilityButtons[i].GetComponentInChildren<TMP_Text>().text =
                cooldown > 0 ? $"{ability.abilityName} ({cooldown})" : ability.abilityName;
        }

        panel.SetActive(true);
    }

    private void HandleUnitDeselected()
    {
        _unit = null;
        panel.SetActive(false);
    }

    /// <summary>Switches the selected unit to movement mode. Wire this to the Move button.</summary>
    public void OnMoveClicked() => _selection.EnterMoveMode();

    /// <summary>Switches the selected unit to targeting with the ability at <paramref name="index"/>. Wire each ability button with its index.</summary>
    public void OnAbilityClicked(int index)
    {
        if (_unit != null && index < _unit.Abilities.Count)
            _selection.EnterAbilityMode(_unit.Abilities[index]);
    }
}