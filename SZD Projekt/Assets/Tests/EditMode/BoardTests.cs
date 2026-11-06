using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BoardTests
{
    private Board _board;

    [SetUp]
    public void SetUp() => _board = new Board(5, 4);

    private UnitState PlaceUnit(int x, int y, PlayerID owner = PlayerID.Player1)
    {
        var unit = new UnitState(owner, 10);
        _board.Place(unit, new Vector2Int(x, y));
        return unit;
    }

    // ─── Construction ────────────────────────────────────────────────────────

    [TestCase(0, 4)]
    [TestCase(5, 0)]
    [TestCase(-1, 4)]
    public void Constructor_NonPositiveSize_Throws(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Board(width, height));
    }

    // ─── Adjacency ───────────────────────────────────────────────────────────

    [Test]
    public void GetAdjacent_Middle_ReturnsFourNeighbours()
    {
        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(2, 2), new Vector2Int(2, 0), new Vector2Int(1, 1), new Vector2Int(3, 1) },
            _board.GetAdjacent(new Vector2Int(2, 1)));
    }

    [Test]
    public void GetAdjacent_Corner_ReturnsTwoNeighbours()
    {
        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(1, 0), new Vector2Int(0, 1) },
            _board.GetAdjacent(new Vector2Int(0, 0)));
    }

    [Test]
    public void GetAdjacent_Edge_ReturnsThreeNeighbours()
    {
        Assert.AreEqual(3, _board.GetAdjacent(new Vector2Int(4, 2)).Count);
    }

    // ─── Range ───────────────────────────────────────────────────────────────

    [Test]
    public void GetInRange_FullDiamond_ExcludesCenterByDefault()
    {
        var board = new Board(9, 9);
        var cells = board.GetInRange(new Vector2Int(4, 4), 2);

        Assert.AreEqual(12, cells.Count); // 13-cell diamond minus the center
        CollectionAssert.DoesNotContain(cells, new Vector2Int(4, 4));
        CollectionAssert.DoesNotContain(cells,
            new Vector2Int(6, 6)); // corner of the bounding square, outside the diamond
    }

    [Test]
    public void GetInRange_IncludeCenter_AddsCenter()
    {
        var board = new Board(9, 9);
        Assert.AreEqual(13, board.GetInRange(new Vector2Int(4, 4), 2, excludeCenter: false).Count);
    }

    [Test]
    public void GetInRange_AtCorner_ClipsToBoard()
    {
        // Only the quarter of the range-2 diamond that lies inside the board, minus the center
        CollectionAssert.AreEquivalent(
            new[]
            {
                new Vector2Int(1, 0), new Vector2Int(2, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1),
                new Vector2Int(0, 2)
            },
            _board.GetInRange(new Vector2Int(0, 0), 2));
    }

    // ─── Placement ───────────────────────────────────────────────────────────

    [Test]
    public void Place_SetsOccupantAndPosition()
    {
        UnitState unit = PlaceUnit(1, 1);

        Assert.AreSame(unit, _board.GetUnitAt(new Vector2Int(1, 1)));
        Assert.AreEqual(new Vector2Int(1, 1), unit.Position);
    }

    [Test]
    public void Place_AgainElsewhere_FreesOldCell()
    {
        UnitState unit = PlaceUnit(1, 1);
        _board.Place(unit, new Vector2Int(2, 1));

        Assert.IsNull(_board.GetUnitAt(new Vector2Int(1, 1)));
        Assert.AreSame(unit, _board.GetUnitAt(new Vector2Int(2, 1)));
    }

    [Test]
    public void Place_OnOccupiedCell_Throws()
    {
        PlaceUnit(1, 1);
        var other = new UnitState(PlayerID.Player2, 10);

        Assert.Throws<InvalidOperationException>(() => _board.Place(other, new Vector2Int(1, 1)));
    }

    [Test]
    public void Place_OutOfBounds_Throws()
    {
        var unit = new UnitState(PlayerID.Player1, 10);
        Assert.Throws<ArgumentOutOfRangeException>(() => _board.Place(unit, new Vector2Int(5, 0)));
    }

    [Test]
    public void Remove_FreesCell_AndIgnoresUnitsNotOnBoard()
    {
        UnitState unit = PlaceUnit(1, 1);
        _board.Remove(unit);
        Assert.IsNull(_board.GetUnitAt(new Vector2Int(1, 1)));

        Assert.DoesNotThrow(() => _board.Remove(new UnitState(PlayerID.Player1, 10)));
    }

    [Test]
    public void Remove_UnitNotOnBoard_DoesNotEvictOccupantAtSamePosition()
    {
        UnitState placed = PlaceUnit(0, 0);
        var neverPlaced = new UnitState(PlayerID.Player2, 10); // Position defaults to (0, 0)

        _board.Remove(neverPlaced);

        Assert.AreSame(placed, _board.GetUnitAt(Vector2Int.zero));
    }

    // ─── Movement rule ───────────────────────────────────────────────────────

    [Test]
    public void GetReachable_RangeOne_IsFreeOrthogonalNeighbours()
    {
        UnitState unit = PlaceUnit(2, 2);
        PlaceUnit(3, 2, PlayerID.Player2);

        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(2, 3), new Vector2Int(2, 1), new Vector2Int(1, 2) },
            _board.GetReachable(unit, 1));
    }

    [Test]
    public void GetReachable_RangeTwo_IsDiamondWithoutOwnCell()
    {
        UnitState unit = PlaceUnit(2, 1);

        CollectionAssert.AreEquivalent(_board.GetInRange(new Vector2Int(2, 1), 2), _board.GetReachable(unit, 2));
    }

    [Test]
    public void GetReachable_Corner_StaysInBounds()
    {
        UnitState unit = PlaceUnit(0, 0);

        CollectionAssert.AreEquivalent(
            new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(2, 0), new Vector2Int(1, 1), new Vector2Int(0, 2) },
            _board.GetReachable(unit, 2));
    }

    [Test]
    public void GetReachable_DoesNotPassThroughUnits()
    {
        // Unit in the corner, walled in by an ally and an enemy: nothing is reachable however far it may go
        UnitState unit = PlaceUnit(0, 0);
        PlaceUnit(1, 0);
        PlaceUnit(0, 1, PlayerID.Player2);

        CollectionAssert.IsEmpty(_board.GetReachable(unit, 5));
    }

    [Test]
    public void GetReachable_GoesAroundBlockers_CountingTheDetour()
    {
        // . . .     (2,2) is blocked, so (2,3) costs 4 steps around it, not 2
        // . X .
        // . U .
        UnitState unit = PlaceUnit(2, 1);
        PlaceUnit(2, 2, PlayerID.Player2);

        Assert.IsFalse(_board.GetReachable(unit, 3).Contains(new Vector2Int(2, 3)));
        Assert.IsTrue(_board.GetReachable(unit, 4).Contains(new Vector2Int(2, 3)));
    }

    [Test]
    public void GetReachable_RangeZero_IsEmpty()
    {
        CollectionAssert.IsEmpty(_board.GetReachable(PlaceUnit(2, 2), 0));
    }

    [Test]
    public void FindPath_ReturnsShortestStepsEndingAtTarget()
    {
        UnitState unit = PlaceUnit(2, 1);
        PlaceUnit(2, 2, PlayerID.Player2);

        List<Vector2Int> path = _board.FindPath(unit, new Vector2Int(2, 3), 4);

        Assert.AreEqual(4, path.Count);
        Assert.AreEqual(new Vector2Int(2, 3), path[path.Count - 1]);
        Vector2Int previous = unit.Position;
        foreach (Vector2Int step in path)
        {
            Assert.AreEqual(1, Mathf.Abs(step.x - previous.x) + Mathf.Abs(step.y - previous.y), "not one orthogonal step");
            Assert.IsNull(_board.GetUnitAt(step), "walked through a unit");
            previous = step;
        }
    }

    [TestCase(2, 2)] // own cell
    [TestCase(2, 3)] // occupied
    [TestCase(-1, 1)] // out of bounds
    public void FindPath_Unreachable_ReturnsNull(int x, int y)
    {
        UnitState unit = PlaceUnit(2, 2);
        PlaceUnit(2, 3, PlayerID.Player2);

        Assert.IsNull(_board.FindPath(unit, new Vector2Int(x, y), 1));
    }
}
