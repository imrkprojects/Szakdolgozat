using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>Checks that GameBootstrap's Inspector references and wiring produce a running match.</summary>
// ReSharper disable once CheckNamespace -- the project has no namespaces anywhere
public class GameSceneSmokeTests
{
    [UnityTest]
    public IEnumerator GameScene_StartsInPlanning_AndSelectsFirstUnitAfterCommit()
    {
        yield return SceneManager.LoadSceneAsync("GameScene");
        yield return null; // let GameBootstrap.Start run

        // Find* is fine here: the no-globals rule is for game code, and tests have no composition root
        var bootstrap = Object.FindAnyObjectByType<GameBootstrap>();
        Assert.IsNotNull(bootstrap, "GameScene has no GameBootstrap");
        Match match = bootstrap.Match;
        Assert.IsNotNull(match, "GameBootstrap did not create a match");

        // Each player gets one unit per lineup entry, in lineup order, with that type's HP
        AssertSpawnedLineup(match, PlayerID.Player1, bootstrap.Player1Lineup);
        AssertSpawnedLineup(match, PlayerID.Player2, bootstrap.Player2Lineup);
        Assert.AreEqual(CyclePhase.Planning, match.Turns.Phase);
        Assert.IsNull(match.Turns.ActiveUnit);

        match.Turns.CommitPlan(PlayerID.Player1, match.Turns.GetLivingUnits(PlayerID.Player1));
        match.Turns.CommitPlan(PlayerID.Player2, match.Turns.GetLivingUnits(PlayerID.Player2));
        Assert.IsNotNull(match.Turns.ActiveUnit);
        Assert.AreEqual(PlayerID.Player1, match.Turns.ActiveUnit.Owner);
        Assert.IsFalse(match.Turns.IsGameOver);
        Assert.AreEqual(match.Turns.ActiveUnit, Object.FindAnyObjectByType<SelectionManager>().SelectedUnit.State,
            "SelectionManager did not select the first active unit");

        // Every painted structure is on the board with its type, and no unit stands on a wall
        var grid = Object.FindAnyObjectByType<GridManager>();
        List<(Vector2Int cell, StructureData data)> painted = grid.GetPaintedStructures();
        Assert.IsNotEmpty(painted, "GameScene has no painted structures");
        foreach ((Vector2Int cell, StructureData data) in painted)
        {
            Assert.AreSame(data, match.Board.GetStructureAt(cell)?.Data, $"structure at {cell} is not on the board");
            if (data.blocksMovement) Assert.IsNull(match.Board.GetUnitAt(cell), $"a unit stands on the wall at {cell}");
        }

        // Selecting enters move mode: the overlay marks the unit's cell and every move target
        UnitState active = match.Turns.ActiveUnit;
        Assert.AreEqual(TileHighlightState.Selected, grid.GetHighlight(active.Position), "unit cell highlight");
        foreach (Vector2Int cell in match.GetMoveTargets(active))
            Assert.AreEqual(TileHighlightState.Reachable, grid.GetHighlight(cell), $"move target {cell} highlight");

        // Cell (x, y) stays centered at (x, y) * 1.1, so units and the camera keep their layout
        Vector3 corner = grid.CellToWorld(new Vector2Int(grid.Width - 1, grid.Height - 1));
        Assert.That(Vector3.Distance(new Vector3((grid.Width - 1) * 1.1f, (grid.Height - 1) * 1.1f, 0f), corner),
            Is.LessThan(0.001f), $"cell center moved to {corner}");

        // The action bar shows one labelled button per ability of the selected unit and hides the rest
        IReadOnlyList<AbilityData> abilities = match.Turns.ActiveUnit.Abilities;
        for (int i = 1; i <= 4; i++)
        {
            GameObject button = GameObject.Find($"AbilityButton{i}");
            bool shouldShow = i <= abilities.Count;
            Assert.AreEqual(shouldShow, button != null && button.activeInHierarchy, $"AbilityButton{i} visibility");
            if (shouldShow)
                Assert.AreEqual(abilities[i - 1].abilityName,
                    button.GetComponentInChildren<TMPro.TMP_Text>().text, $"AbilityButton{i} label");
        }

        // Hit feedback is wired for both units and structures: each HP change spawns one floating number
        active.TakeDamage(1);
        Assert.AreEqual(1, Object.FindObjectsByType<FloatingText>().Length, "unit hit");
        (Vector2Int wallCell, _) = painted.First(p => p.data.destructible);
        match.Board.GetStructureAt(wallCell).TakeDamage(1);
        Assert.AreEqual(2, Object.FindObjectsByType<FloatingText>().Length, "wall hit");
    }

    private static void AssertSpawnedLineup(Match match, PlayerID owner, IReadOnlyList<UnitData> lineup)
    {
        IReadOnlyList<UnitState> units = match.Turns.GetLivingUnits(owner);
        Assert.Greater(lineup.Count, 0, $"{owner} has an empty lineup");
        Assert.AreEqual(lineup.Count, units.Count, $"{owner} spawned the wrong number of units");
        for (int i = 0; i < lineup.Count; i++)
        {
            Assert.AreEqual(lineup[i].maxHp, units[i].MaxHp, $"{owner} unit {i + 1} has the wrong HP");
            Assert.AreEqual(lineup[i].moveRange, units[i].MoveRange, $"{owner} unit {i + 1} has the wrong move range");
            Assert.AreSame(units[i], match.Board.GetUnitAt(units[i].Position),
                $"{owner} unit {i + 1} is not on the board");
        }
    }

    // Stands in for the M4 playtest: drives the real scene through Core, so view wiring errors fail the test
    [UnityTest]
    public IEnumerator GameScene_TenCyclesWithDeathAndToken_NeverStall()
    {
        yield return SceneManager.LoadSceneAsync("GameScene");
        yield return null;

        Match match = Object.FindAnyObjectByType<GameBootstrap>().Match;
        var selection = Object.FindAnyObjectByType<SelectionManager>();
        TurnCycle turns = match.Turns;
        bool tokenSpent = false, killed = false;

        bool freeReorderTaken = false;

        for (int step = 0; turns.GetCycle(PlayerID.Player1) <= 10; step++)
        {
            Assert.Less(step, 500, $"turn flow stalled in {turns.Phase}");
            Assert.IsFalse(turns.IsGameOver);
            if (turns.Phase == CyclePhase.Planning)
            {
                turns.CommitPlan(PlayerID.Player1, turns.GetLivingUnits(PlayerID.Player1));
                turns.CommitPlan(PlayerID.Player2, turns.GetLivingUnits(PlayerID.Player2));
                yield return null;
                continue;
            }

            Assert.AreSame(turns.ActiveUnit, selection.SelectedUnit?.State, "selection lost the active unit");
            PlayerID active = turns.ActivePlayer;

            // Take the free death reorder, and spend one token, so both reorder paths run through the views
            if (turns.CanReorder(active) && (turns.ReorderIsFree(active) || !tokenSpent))
            {
                if (turns.ReorderIsFree(active)) freeReorderTaken = true;
                else tokenSpent = true;
                List<UnitState> order = turns.GetLivingUnits(active).Reverse().ToList();
                Assert.IsTrue(turns.TryReorder(active, order));
                yield return null;
                continue;
            }

            // Kill an enemy unit once while its owner has 3, so they get a free reorder
            PlayerID enemy = active == PlayerID.Player1 ? PlayerID.Player2 : PlayerID.Player1;
            if (!killed && turns.GetLivingUnits(enemy).Count == 3)
            {
                UnitState victim = turns.GetLivingUnits(enemy)[0];
                victim.TakeDamage(victim.Hp);
                killed = true;
            }

            match.EndTurn();
            yield return null;
        }

        Assert.IsTrue(tokenSpent, "no token was spent");
        Assert.IsTrue(killed, "no unit died");
        Assert.IsTrue(freeReorderTaken, "no free reorder was taken");
    }
}