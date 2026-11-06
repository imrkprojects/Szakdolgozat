using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Composition root: the only place that creates and wires the match's objects.</summary>
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private Unit unitPrefab;

    [Header("Lineups (one unit per entry)")] [SerializeField]
    private UnitData[] player1Lineup;

    [SerializeField] private UnitData[] player2Lineup;

    [Header("Scene objects")] [SerializeField]
    private GridManager grid;

    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private ActionBarUI actionBarUI;
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private PlayerStatusUI playerStatusUI;
    [SerializeField] private TurnOrderUI turnOrderUI;

    /// <summary>The running match; exposed for PlayMode tests.</summary>
    public Match Match { get; private set; }

    /// <summary>Unit types Player 1 starts with; exposed for PlayMode tests.</summary>
    public IReadOnlyList<UnitData> Player1Lineup => player1Lineup;

    /// <summary>Unit types Player 2 starts with; exposed for PlayMode tests.</summary>
    public IReadOnlyList<UnitData> Player2Lineup => player2Lineup;

    private void Start()
    {
        if (!IsValidLineup(player1Lineup) || !IsValidLineup(player2Lineup))
        {
            Debug.LogError("GameBootstrap: each player needs a non-empty lineup with no empty slots, " +
                           "no larger than the grid width. The match was not started.");
            return;
        }

        Match = new Match(grid.Width, grid.Height);
        Debug.Log($"Players initialized: {Match.Player1.Name} vs {Match.Player2.Name}");
        // The turn order panel is IMGUI, which board clicks don't see, so clicks on it are dropped here
        grid.BuildGrid(Match.Board, cell =>
        {
            if (!turnOrderUI.IsPointerOverPanel) selectionManager.HandleCellClicked(cell);
        });

        // Before the units, so a wall painted on a spawn cell makes Board.Place throw
        var structures = new List<StructureState>();
        foreach ((Vector2Int cell, StructureData data) in grid.GetPaintedStructures())
        {
            StructureState structure = data.CreateState();
            Match.AddStructure(structure, cell);
            structures.Add(structure);
        }

        grid.TrackStructures(structures);

        // Everything that reacts to selection or turns subscribes before the match starts and fires the first turn
        selectionManager.Init(Match, grid);
        cameraController.Init(grid, selectionManager);
        actionBarUI.Init(selectionManager, Match);
        gameOverUI.Init(Match, selectionManager);
        playerStatusUI.Init(Match);
        turnOrderUI.Init(Match, grid);

        SpawnInitialUnits();
        Match.Start();
        Debug.Log("Match started.");
    }

    // shortcut: one row per player, so a lineup can't exceed the grid width; add more rows if lineups grow
    private bool IsValidLineup(UnitData[] lineup) =>
        lineup != null && lineup.Length > 0 && lineup.Length <= grid.Width && Array.IndexOf(lineup, null) < 0;

    // TODO: Move to a separate UnitSpawner class.
    private void SpawnInitialUnits()
    {
        int w = grid.Width;
        int h = grid.Height;

        // Place Player 1 along the bottom row.
        for (int i = 0; i < player1Lineup.Length; i++)
            SpawnUnit(player1Lineup[i], PlayerID.Player1, i + 1, new Vector2Int(i, 0));

        // Place Player 2 along the top row, starting at the opposite edge.
        for (int i = 0; i < player2Lineup.Length; i++)
            SpawnUnit(player2Lineup[i], PlayerID.Player2, i + 1, new Vector2Int(w - 1 - i, h - 1));
    }

    private void SpawnUnit(UnitData data, PlayerID owner, int number, Vector2Int pos)
    {
        Unit unit = Instantiate(unitPrefab);
        unit.name = $"{(owner == PlayerID.Player1 ? "P1" : "P2")}_{number}_{data.unitName}";
        unit.Init(data, owner, Match.GetPlayer(owner).Color, grid);
        unit.SetNumber(number);
        Match.AddUnit(unit.State, pos);
        unit.PlaceAt(pos);
        playerStatusUI.AddUnit(unit);
    }
}