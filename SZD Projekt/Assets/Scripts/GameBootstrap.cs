using UnityEngine;

/// <summary>Composition root: the only place that creates and wires the match's objects.</summary>
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private Unit unitPrefab;

    [Header("Scene objects")] [SerializeField]
    private GridManager grid;

    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private ActionBarUI actionBarUI;
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private PlayerStatusUI playerStatusUI;

    /// <summary>The running match; exposed for PlayMode tests.</summary>
    public Match Match { get; private set; }

    private void Start()
    {
        Match = new Match(grid.Width, grid.Height);
        Debug.Log($"Players initialized: {Match.Player1.Name} vs {Match.Player2.Name}");
        grid.BuildGrid(Match.Board, selectionManager.HandleTileClicked);

        // Everything that reacts to selection or turns subscribes before the match starts and fires the first turn
        selectionManager.Init(Match, grid);
        cameraController.Init(grid, selectionManager);
        actionBarUI.Init(selectionManager);
        gameOverUI.Init(Match, selectionManager);
        playerStatusUI.Init(Match);

        SpawnInitialUnits();
        Match.Start();
        Debug.Log("Match started.");
    }

    // TODO: Move to a separate UnitSpawner class.
    private void SpawnInitialUnits()
    {
        int w = grid.Width;
        int h = grid.Height;

        // Place Player 1 along the bottom row.
        for (int i = 0; i < 3; i++)
            SpawnUnit(PlayerID.Player1, i + 1, new Vector2Int(i, 0));

        // Place Player 2 along the top row, starting at the opposite edge.
        for (int i = 0; i < 3; i++)
            SpawnUnit(PlayerID.Player2, i + 1, new Vector2Int(w - 1 - i, h - 1));
    }

    private void SpawnUnit(PlayerID owner, int number, Vector2Int pos)
    {
        Unit unit = Instantiate(unitPrefab);
        unit.name = $"{(owner == PlayerID.Player1 ? "P1" : "P2")}_Unit{number}";
        unit.Init(owner, Match.GetPlayer(owner).Color, grid);
        unit.SetNumber(number);
        Match.AddUnit(unit.State, pos);
        unit.PlaceOnTile(grid.GetTile(pos));
        playerStatusUI.AddUnit(unit);
    }
}