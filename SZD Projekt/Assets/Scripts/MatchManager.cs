using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    [SerializeField] private Unit unitPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        PlayerManager.Instance.InitPlayers();
        GridManager.Instance.BuildGrid();
        SpawnInitialUnits();
        TurnManager.Instance.StartGame();
        Debug.Log("Match started.");
    }

    // TODO: Move to a separate UnitSpawner class
    private void SpawnInitialUnits()
    {
        int w = GridManager.Instance.Width;
        int h = GridManager.Instance.Height;

        // Player 1 — bottom row (x = 0, 1, 2  |  y = 0)
        for (int i = 0; i < 3; i++)
        {
            Unit unit = Instantiate(unitPrefab);
            unit.name = $"P1_Unit{i + 1}";
            unit.SetOwner(PlayerID.Player1);
            unit.PlaceOnTile(GridManager.Instance.GetTile(new Vector2Int(i, 0)));
            TurnManager.Instance.RegisterUnit(unit);
        }

        // Player 2 — top row (x = width-1, width-2, width-3  |  y = height-1)
        for (int i = 0; i < 3; i++)
        {
            Unit unit = Instantiate(unitPrefab);
            unit.name = $"P2_Unit{i + 1}";
            unit.SetOwner(PlayerID.Player2);
            unit.PlaceOnTile(GridManager.Instance.GetTile(new Vector2Int(w - 1 - i, h - 1)));
            TurnManager.Instance.RegisterUnit(unit);
        }
    }
}