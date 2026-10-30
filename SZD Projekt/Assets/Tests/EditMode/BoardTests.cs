using System;
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
    public void CanMoveTo_EmptyOrthogonalNeighbour_True()
    {
        UnitState unit = PlaceUnit(2, 2);
        Assert.IsTrue(_board.CanMoveTo(unit, new Vector2Int(2, 3)));
    }

    [Test]
    public void CanMoveTo_OccupiedNeighbour_False()
    {
        UnitState unit = PlaceUnit(2, 2);
        PlaceUnit(3, 2, PlayerID.Player2);
        Assert.IsFalse(_board.CanMoveTo(unit, new Vector2Int(3, 2)));
    }

    [TestCase(3, 3)] // diagonal
    [TestCase(4, 2)] // two steps
    [TestCase(2, 2)] // own cell
    public void CanMoveTo_NotOneOrthogonalStep_False(int x, int y)
    {
        UnitState unit = PlaceUnit(2, 2);
        Assert.IsFalse(_board.CanMoveTo(unit, new Vector2Int(x, y)));
    }

    [Test]
    public void CanMoveTo_OutOfBounds_False()
    {
        UnitState unit = PlaceUnit(0, 0);
        Assert.IsFalse(_board.CanMoveTo(unit, new Vector2Int(-1, 0)));
    }
}