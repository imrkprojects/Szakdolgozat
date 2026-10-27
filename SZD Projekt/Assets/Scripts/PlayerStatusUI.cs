using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>Shows each player's name and their units' health, or "Dead" for fallen units.</summary>
/// <remarks>Attach this to an always-active object so it can subscribe before units are spawned.</remarks>
public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] private TMP_Text player1Text; // Player1StatusText
    [SerializeField] private TMP_Text player2Text; // Player2StatusText

    // Dead units stay in the list so they can be shown as "Dead"; only their managed fields are read after Destroy
    private readonly List<Unit> _units = new List<Unit>();

    private void OnEnable()
    {
        Unit.OnUnitPlaced += HandleUnitPlaced;
        Unit.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        Unit.OnUnitPlaced -= HandleUnitPlaced;
        Unit.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleUnitPlaced(Unit unit)
    {
        // OnUnitPlaced also fires on every move, so only the first placement adds the unit
        if (_units.Contains(unit)) return;
        _units.Add(unit);
        Refresh();
    }

    private void HandleHealthChanged(Unit unit) => Refresh();

    private void Refresh()
    {
        player1Text.text = BuildStatus(PlayerID.Player1);
        player2Text.text = BuildStatus(PlayerID.Player2);
    }

    private string BuildStatus(PlayerID id)
    {
        Player player = PlayerManager.Instance.GetPlayer(id);
        var sb = new StringBuilder($"<color=#{ColorUtility.ToHtmlStringRGB(player.Color)}>{player.Name}</color>\n");
        foreach (Unit unit in _units)
        {
            if (unit.OwnerID != id) continue;
            string status = unit.CurrentHp > 0 ? $"{unit.CurrentHp}/{unit.MaxHp} HP" : "Dead";
            sb.AppendLine($"Unit {unit.unitID}: {status}");
        }

        return sb.ToString();
    }
}
