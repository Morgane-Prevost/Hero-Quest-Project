using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Board
{
    public enum ElementKind { Stairs, Door, Blocked, Furniture, Monster, Trap }

    /// <summary>Pure description (no GameObject) of one level element. Read-only by convention.</summary>
    public sealed class ElementDef
    {
        public string id, type, note;
        public ElementKind kind;
        public int x, y, w, h;                       // footprint (door: reference cell, 1x1)
        public Dir facing;                           // furniture / monsters
        public Dir side;                             // doors: side of cell (x,y)
        public EdgeKey edge;                         // doors
        public bool hidden, trapped;
        public List<Vector2Int> cells = new List<Vector2Int>();   // covered cells (door: its 2 neighbours)
        public List<int> zones = new List<int>();                 // touched zones

        public bool BlocksMovement { get { return kind == ElementKind.Blocked || kind == ElementKind.Furniture; } }

        public bool Covers(int cx, int cy)
        {
            for (int i = 0; i < cells.Count; i++) if (cells[i].x == cx && cells[i].y == cy) return true;
            return false;
        }
    }

    /// <summary>
    /// Read-only level model: walls + elements + unused rooms.
    /// This is what the game logic queries (never the GameObjects).
    /// </summary>
    public sealed class LevelModel
    {
        public readonly string id, name;
        readonly HashSet<EdgeKey> wallSet;
        readonly List<EdgeKey> wallList;
        readonly List<ElementDef> elements;
        readonly List<int> initialZones;
        readonly List<int> unusedZones;
        readonly HashSet<int> unusedSet;
        readonly Dictionary<string, ElementDef> byId = new Dictionary<string, ElementDef>();
        readonly HashSet<Vector2Int> blockedCells = new HashSet<Vector2Int>();
        readonly List<Vector2Int> stairsCells = new List<Vector2Int>();

        public IReadOnlyList<EdgeKey> WallEdges { get { return wallList; } }
        public IReadOnlyList<ElementDef> Elements { get { return elements; } }
        public IReadOnlyList<Vector2Int> StairsCells { get { return stairsCells; } }
        public IReadOnlyList<int> InitialZones { get { return initialZones; } }
        public IReadOnlyList<int> UnusedZones { get { return unusedZones; } }

        public LevelModel(string id, string name, List<EdgeKey> walls, List<ElementDef> elements,
                          List<int> initialZones, List<int> unusedZones)
        {
            this.id = id; this.name = name;
            wallList = walls; wallSet = new HashSet<EdgeKey>(walls);
            this.elements = elements; this.initialZones = initialZones;
            this.unusedZones = unusedZones; unusedSet = new HashSet<int>(unusedZones);
            foreach (var e in elements)
            {
                byId[e.id] = e;
                if (e.BlocksMovement) foreach (var c in e.cells) blockedCells.Add(c);
                if (e.kind == ElementKind.Stairs) stairsCells.AddRange(e.cells);
            }
        }

        public bool HasWall(EdgeKey e) { return wallSet.Contains(e); }
        public bool HasWall(int x, int y, Dir side) { return wallSet.Contains(EdgeKey.FromCell(x, y, side)); }

        public ElementDef GetElement(string id) { ElementDef d; return byId.TryGetValue(id, out d) ? d : null; }

        /// <summary>Cell covered by furniture or a blocked square (impassable).</summary>
        public bool IsBlocked(int x, int y) { return blockedCells.Contains(new Vector2Int(x, y)); }

        public bool IsUnusedZone(int zone) { return unusedSet.Contains(zone); }

        /// <summary>True if the cell exists on the board and belongs to a room/corridor used by this level.</summary>
        public bool IsPlayable(BoardModel board, int x, int y)
        {
            int z = board.ZoneAt(x, y);
            return z >= 0 && !unusedSet.Contains(z);
        }

        public IEnumerable<ElementDef> ElementsAt(int x, int y)
        {
            foreach (var e in elements) if (e.Covers(x, y)) yield return e;
        }

        /// <summary>Door placed on this edge (null if none).</summary>
        public ElementDef DoorAt(EdgeKey edge)
        {
            foreach (var e in elements) if (e.kind == ElementKind.Door && e.edge.Equals(edge)) return e;
            return null;
        }
    }
}