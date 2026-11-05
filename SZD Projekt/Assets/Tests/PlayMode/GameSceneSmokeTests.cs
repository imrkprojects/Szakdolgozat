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

        int p1 = 0, p2 = 0;
        for (int x = 0; x < match.Board.Width; x++)
        for (int y = 0; y < match.Board.Height; y++)
        {
            UnitState unit = match.Board.GetUnitAt(new Vector2Int(x, y));
            if (unit?.Owner == PlayerID.Player1) p1++;
            else if (unit?.Owner == PlayerID.Player2) p2++;
        }

        Assert.AreEqual(3, p1);
        Assert.AreEqual(3, p2);
        Assert.AreEqual(CyclePhase.Planning, match.Turns.Phase);
        Assert.IsNull(match.Turns.ActiveUnit);

        match.Turns.CommitPlan(PlayerID.Player1, match.Turns.GetLivingUnits(PlayerID.Player1));
        match.Turns.CommitPlan(PlayerID.Player2, match.Turns.GetLivingUnits(PlayerID.Player2));
        Assert.IsNotNull(match.Turns.ActiveUnit);
        Assert.AreEqual(PlayerID.Player1, match.Turns.ActiveUnit.Owner);
        Assert.IsFalse(match.Turns.IsGameOver);
        Assert.AreEqual(match.Turns.ActiveUnit, Object.FindAnyObjectByType<SelectionManager>().SelectedUnit.State,
            "SelectionManager did not select the first active unit");
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