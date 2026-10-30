using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>Checks that GameBootstrap's Inspector references and wiring produce a running match.</summary>
// ReSharper disable once CheckNamespace -- the project has no namespaces anywhere
public class GameSceneSmokeTests
{
    [UnityTest]
    public IEnumerator GameScene_StartsMatchWithThreeUnitsPerPlayer()
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
        Assert.IsNotNull(match.Turns.ActiveUnit);
        Assert.AreEqual(PlayerID.Player1, match.Turns.ActiveUnit.Owner);
        Assert.IsFalse(match.Turns.IsGameOver);
        Assert.AreEqual(match.Turns.ActiveUnit, Object.FindAnyObjectByType<SelectionManager>().SelectedUnit.State,
            "SelectionManager did not select the first active unit");
    }
}