using UnityEngine;

/// <summary>Initializes a match by creating players, the grid, and starting units.</summary>
public class MatchManager : MonoBehaviour
{
    /// <summary>The active match manager instance.</summary>
    public static MatchManager Instance { get; private set; }

    [SerializeField] private Unit unitPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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

    // TODO: Move to a separate UnitSpawner class.
    private void SpawnInitialUnits()
    {
        int w = GridManager.Instance.Width;
        int h = GridManager.Instance.Height;

        // Place Player 1 along the bottom row.
        for (int i = 0; i < 3; i++)
        {
            Unit unit = Instantiate(unitPrefab);
            unit.name = $"P1_Unit{i + 1}";
            unit.SetOwner(PlayerID.Player1);
            unit.SetNumber(i + 1);
            unit.PlaceOnTile(GridManager.Instance.GetTile(new Vector2Int(i, 0)));
            TurnManager.Instance.RegisterUnit(unit);
        }

        // Place Player 2 along the top row, starting at the opposite edge.
        for (int i = 0; i < 3; i++)
        {
            Unit unit = Instantiate(unitPrefab);
            unit.name = $"P2_Unit{i + 1}";
            unit.SetOwner(PlayerID.Player2);
            unit.SetNumber(i + 1);
            unit.PlaceOnTile(GridManager.Instance.GetTile(new Vector2Int(w - 1 - i, h - 1)));
            TurnManager.Instance.RegisterUnit(unit);
        }
    }
}