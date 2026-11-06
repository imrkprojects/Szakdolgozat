using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Hot-seat planning: each player orders their units behind a hand-over screen and commits.
/// During a player's turn it offers the reorder Core allows (free after a death, or a token at a cycle start);
/// the player may dismiss it or simply act. The HUD shows both queues, cycles and tokens.
/// </summary>
// shortcut: IMGUI placeholder look so no scene panel is needed; replace with a uGUI panel
public class TurnOrderUI : MonoBehaviour
{
    private const float ReferenceHeight = 720f;

    private Match _match;
    private GridManager _grid;

    // Planning: player whose order is being edited, or None; the hand-over screen hides it until they confirm
    private PlayerID _planner = PlayerID.None;
    private bool _handOver;
    private List<UnitState> _order = new List<UnitState>();

    // Acting: the player chose to keep their order this turn
    private bool _offerDismissed;

    // Panels drawn last frame, in scaled GUI space; IMGUI does not stop OnMouseDown on the tiles below
    private readonly List<Rect> _panels = new List<Rect>();
    private Vector2 _mouse;

    // Created in OnGUI: GUI.skin is only available there
    private GUIStyle _richLabel;
    private GUIStyle _banner;

    /// <summary>Whether the mouse is over one of this UI's panels, so a click there is not a tile click.</summary>
    public bool IsPointerOverPanel => _panels.Any(rect => rect.Contains(_mouse));

    /// <summary>Links the panel to the match; call before the match starts so the planning phase is caught.</summary>
    public void Init(Match match, GridManager grid)
    {
        _match = match;
        _grid = grid;
        _match.Turns.PhaseChanged += HandlePhaseChanged;
        _match.Turns.TurnChanged += HandleTurnChanged;
    }

    private void OnDestroy()
    {
        if (_match == null) return;
        _match.Turns.PhaseChanged -= HandlePhaseChanged;
        _match.Turns.TurnChanged -= HandleTurnChanged;
    }

    private void HandlePhaseChanged(CyclePhase phase)
    {
        if (phase == CyclePhase.Planning) BeginPlanning(PlayerID.Player1);
        else _planner = PlayerID.None;
    }

    // A new turn, or a reorder that changed the active unit: offer the current order again
    private void HandleTurnChanged(PlayerID player, UnitState unit)
    {
        _offerDismissed = false;
        _order = new List<UnitState>(_match.Turns.GetLivingUnits(player));
    }

    private void BeginPlanning(PlayerID player)
    {
        _planner = player;
        _handOver = true;
        _order = new List<UnitState>(_match.Turns.GetLivingUnits(player));
    }

    private void OnGUI()
    {
        if (_match == null) return;
        float scale = Screen.height / ReferenceHeight;
        if (Event.current.type == EventType.Repaint)
        {
            _mouse = Event.current.mousePosition / scale;
            _panels.Clear();
        }

        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;

        _richLabel ??= new GUIStyle(GUI.skin.label) { richText = true };
        _banner ??= new GUIStyle(_richLabel)
            { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };
        DrawBanner(width);

        // Read once: a click below can change the phase, and IMGUI needs the same panels as in the layout pass
        TurnCycle turns = _match.Turns;
        CyclePhase phase = turns.Phase;
        bool offer = phase == CyclePhase.Acting && !_offerDismissed && turns.CanReorder(turns.ActivePlayer);
        // Not during Planning: a committed order must stay hidden until both players have committed
        if (phase == CyclePhase.Acting) DrawHud(width);

        if (phase == CyclePhase.Planning && _planner != PlayerID.None)
        {
            BeginPanel(OrderPanel(width));
            if (_handOver) DrawHandOver(_match.GetPlayer(_planner));
            else DrawPlanning(_match.GetPlayer(_planner));
            GUILayout.EndArea();
        }
        else if (offer)
        {
            BeginPanel(OrderPanel(width));
            DrawReorderOffer(_match.GetPlayer(turns.ActivePlayer));
            GUILayout.EndArea();
        }
    }

    private Rect OrderPanel(float width) => new Rect(width / 2 - 170, 120, 340, 110 + _order.Count * 30);

    private void BeginPanel(Rect rect)
    {
        if (Event.current.type == EventType.Repaint) _panels.Add(rect);
        GUILayout.BeginArea(rect, GUI.skin.box);
    }

    private void DrawHandOver(Player player)
    {
        GUILayout.Label($"Hand over to {player.Name}");
        if (GUILayout.Button($"I am {player.Name}: show my units")) _handOver = false;
    }

    private void DrawPlanning(Player player)
    {
        GUILayout.Label($"{player.Name}, order your units");
        DrawOrderList();
        if (!GUILayout.Button("Commit")) return;

        _match.Turns.CommitPlan(_planner, _order);
        // Still planning means Player 2 has not committed yet
        if (_match.Turns.Phase == CyclePhase.Planning) BeginPlanning(PlayerID.Player2);
    }

    private void DrawReorderOffer(Player player)
    {
        TurnCycle turns = _match.Turns;
        bool free = turns.ReorderIsFree(player.ID);
        GUILayout.Label(free
            ? $"{player.Name}: a unit died. Reorder for free? A new order starts a new cycle now."
            : $"{player.Name}: new cycle. Reorder for 1 token? ({turns.GetTokens(player.ID)} left)", _richLabel);
        // A token reorder to the same order would change nothing, so it is only offered for a new order.
        // Read before the list: a swap there must not add a button the layout pass did not have
        bool changed = !_order.SequenceEqual(turns.GetLivingUnits(player.ID));
        DrawOrderList();

        if (GUILayout.Button(free ? "Keep current queue" : "Keep order")) _offerDismissed = true;
        else if ((free || changed) &&
                 GUILayout.Button(free ? "Start new cycle with this order" : "Reorder (spends 1 token)"))
            turns.TryReorder(player.ID, _order); // TurnChanged resets the offer and selects the new first unit
    }

    private void DrawOrderList()
    {
        for (int i = 0; i < _order.Count; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{i + 1}. {Label(_order[i])}");
            if (GUILayout.Button("▲", GUILayout.Width(30)) && i > 0) Swap(i, i - 1);
            if (GUILayout.Button("▼", GUILayout.Width(30)) && i < _order.Count - 1) Swap(i, i + 1);
            GUILayout.EndHorizontal();
        }
    }

    // "Player X acting, cycle N"; no clicks, so it is not a panel
    private void DrawBanner(float width)
    {
        TurnCycle turns = _match.Turns;
        string text = turns.Phase switch
        {
            CyclePhase.Planning => "Plan your unit order",
            CyclePhase.Acting => $"{Name(turns.ActivePlayer)} acting, cycle {turns.GetCycle(turns.ActivePlayer)}",
            _ => null
        };
        if (text == null) return;
        GUI.Label(new Rect(width / 2 - 200, 10, 400, 30), text, _banner);
    }

    // Both orders are public once committed; shows what is left of each cycle and the tokens,
    // with the acting player's unit highlighted
    private void DrawHud(float width)
    {
        PlayerID active = _match.Turns.ActivePlayer;
        BeginPanel(new Rect(width - 260, 10, 250, 120));
        foreach (Player player in new[] { _match.Player1, _match.Player2 })
        {
            IEnumerable<string> labels = _match.Turns.GetQueue(player.ID)
                .Select((unit, i) =>
                    player.ID == active && i == 0 ? $"<color=yellow><b>{Label(unit)}</b></color>" : Label(unit));
            GUILayout.Label(
                $"{Name(player.ID)} cycle {_match.Turns.GetCycle(player.ID)}, {_match.Turns.GetTokens(player.ID)} tokens: {string.Join(", ", labels)}",
                _richLabel);
        }

        GUILayout.EndArea();
    }

    private void Swap(int a, int b) => (_order[a], _order[b]) = (_order[b], _order[a]);

    // Player name in the player's color
    private string Name(PlayerID id)
    {
        Player player = _match.GetPlayer(id);
        return $"<color=#{ColorUtility.ToHtmlStringRGB(player.Color)}>{player.Name}</color>";
    }

    private string Label(UnitState unit)
    {
        Unit view = _grid.GetUnitView(unit);
        return $"{view.unitID} {view.Data.unitName}";
    }
}