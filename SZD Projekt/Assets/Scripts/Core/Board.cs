using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The grid as game state: its size, which unit and structure stands where, and the movement and range rules.</summary>
public class Board
{
    private static readonly Vector2Int[] Directions =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private readonly Dictionary<Vector2Int, UnitState> _occupants = new Dictionary<Vector2Int, UnitState>();
    private readonly Dictionary<Vector2Int, StructureState> _structures = new Dictionary<Vector2Int, StructureState>();

    public int Width { get; }
    public int Height { get; }

    public Board(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Board dimensions must be positive.");
        Width = width;
        Height = height;
    }

    public bool InBounds(Vector2Int pos) => pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;

    /// <summary>Returns the unit at the given cell, or null if it is empty.</summary>
    public UnitState GetUnitAt(Vector2Int pos)
    {
        _occupants.TryGetValue(pos, out UnitState unit);
        return unit;
    }

    /// <summary>Puts the unit on a cell, freeing the cell it was on before. Used for both spawning and moving.</summary>
    public void Place(UnitState unit, Vector2Int pos)
    {
        if (!InBounds(pos)) throw new ArgumentOutOfRangeException(nameof(pos), $"{pos} is outside the board.");
        UnitState occupant = GetUnitAt(pos);
        if (occupant != null && occupant != unit) throw new InvalidOperationException($"{pos} is already occupied.");
        if (GetStructureAt(pos)?.Data.blocksMovement == true)
            throw new InvalidOperationException($"{pos} holds a structure that blocks movement.");

        Remove(unit);
        _occupants[pos] = unit;
        unit.Position = pos;
    }

    /// <summary>Takes the unit off the board. Does nothing if it is not on it.</summary>
    public void Remove(UnitState unit)
    {
        if (GetUnitAt(unit.Position) == unit) _occupants.Remove(unit.Position);
    }

    /// <summary>Returns the structure at the given cell, or null if there is none.</summary>
    public StructureState GetStructureAt(Vector2Int pos)
    {
        _structures.TryGetValue(pos, out StructureState structure);
        return structure;
    }

    /// <summary>Puts a structure on a cell without one. A movement-blocking structure can't go where a unit stands.</summary>
    public void PlaceStructure(StructureState structure, Vector2Int pos)
    {
        if (!InBounds(pos)) throw new ArgumentOutOfRangeException(nameof(pos), $"{pos} is outside the board.");
        if (GetStructureAt(pos) != null) throw new InvalidOperationException($"{pos} already holds a structure.");
        if (structure.Data.blocksMovement && GetUnitAt(pos) != null)
            throw new InvalidOperationException($"{pos} is occupied by a unit.");

        _structures[pos] = structure;
        structure.Position = pos;
    }

    /// <summary>Takes the structure off the board. Does nothing if it is not on it.</summary>
    public void RemoveStructure(StructureState structure)
    {
        if (GetStructureAt(structure.Position) == structure) _structures.Remove(structure.Position);
    }

    /// <summary>True if a unit could step onto the cell: in bounds, no unit and no movement-blocking structure.</summary>
    public bool IsWalkable(Vector2Int pos) =>
        InBounds(pos) && GetUnitAt(pos) == null && GetStructureAt(pos)?.Data.blocksMovement != true;

    /// <summary>Returns the in-bounds orthogonal neighbours of a cell.</summary>
    public List<Vector2Int> GetAdjacent(Vector2Int pos)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        foreach (Vector2Int dir in Directions)
        {
            if (InBounds(pos + dir)) result.Add(pos + dir);
        }

        return result;
    }

    // TODO: Expand functionality supporting dynamic ranges and shapes
    /// <summary>Returns all in-bounds cells within a Manhattan distance (diamond shape) of the center.</summary>
    public List<Vector2Int> GetInRange(Vector2Int center, int range, bool excludeCenter = true)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (excludeCenter && dx == 0 && dy == 0) continue;
                // Manhattan distance check — keeps diamond shape, not square
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > range) continue;
                Vector2Int pos = center + new Vector2Int(dx, dy);
                if (InBounds(pos)) result.Add(pos);
            }
        }

        return result;
    }

    /// <summary>Cells the unit can walk to in at most <paramref name="range"/> orthogonal steps through empty cells (not its own cell).</summary>
    public List<Vector2Int> GetReachable(UnitState unit, int range) =>
        new List<Vector2Int>(SearchPaths(unit, range).Keys);

    /// <summary>
    /// Shortest walk from the unit to <paramref name="to"/> through empty cells, excluding the start and including the end,
    /// or null if it is not reachable within <paramref name="range"/> steps.
    /// </summary>
    public List<Vector2Int> FindPath(UnitState unit, Vector2Int to, int range)
    {
        Dictionary<Vector2Int, Vector2Int> cameFrom = SearchPaths(unit, range);
        if (!cameFrom.ContainsKey(to)) return null;

        var path = new List<Vector2Int>();
        for (Vector2Int pos = to; pos != unit.Position; pos = cameFrom[pos]) path.Add(pos);
        path.Reverse();
        return path;
    }

    // Breadth-first search from the unit; maps every reachable cell to the cell it was reached from
    private Dictionary<Vector2Int, Vector2Int> SearchPaths(UnitState unit, int range)
    {
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var frontier = new List<Vector2Int> { unit.Position };
        for (int step = 0; step < range && frontier.Count > 0; step++)
        {
            var next = new List<Vector2Int>();
            foreach (Vector2Int from in frontier)
            {
                foreach (Vector2Int pos in GetAdjacent(from))
                {
                    if (pos == unit.Position || cameFrom.ContainsKey(pos) || !IsWalkable(pos)) continue;
                    cameFrom[pos] = from;
                    next.Add(pos);
                }
            }

            frontier = next;
        }

        return cameFrom;
    }
}