using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>Shows each player's name and their units' health, or "Dead" for fallen units.</summary>
public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] private TMP_Text player1Text; // Player1StatusText
    [SerializeField] private TMP_Text player2Text; // Player2StatusText

    private Match _match;

    // Dead units stay in the list so they can be shown as "Dead"; only their managed fields are read after Destroy
    private readonly List<Unit> _units = new List<Unit>();

    /// <summary>Links the panel to the players whose names and colors it shows.</summary>
    public void Init(Match match)
    {
        _match = match;
    }

    /// <summary>Starts showing a unit's health. Call once per spawned unit.</summary>
    public void AddUnit(Unit unit)
    {
        _units.Add(unit);
        unit.State.HealthChanged += HandleHealthChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        foreach (Unit unit in _units) unit.State.HealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(UnitState unit) => Refresh();

    private void Refresh()
    {
        player1Text.text = BuildStatus(PlayerID.Player1);
        player2Text.text = BuildStatus(PlayerID.Player2);
    }

    private string BuildStatus(PlayerID id)
    {
        Player player = _match.GetPlayer(id);
        var sb = new StringBuilder($"<color=#{ColorUtility.ToHtmlStringRGB(player.Color)}>{player.Name}</color>\n");
        foreach (Unit unit in _units)
        {
            if (unit.OwnerID != id) continue;
            string status = unit.State.IsAlive ? $"{unit.CurrentHp}/{unit.MaxHp} HP" : "Dead";
            sb.AppendLine($"Unit {unit.unitID}: {status}");
        }

        return sb.ToString();
    }
}