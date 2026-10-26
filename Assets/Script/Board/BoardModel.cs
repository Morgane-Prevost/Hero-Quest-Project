using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>
    /// Read-only grid map model (no scene dependency).
    /// Zones: -1 = void, 0 = corridor, 1..N = room N.
    /// Actual walls depend on the level: see LevelModel.
    /// </summary>
    public sealed class BoardModel
    {
        public readonly string id;
        public readonly int width, height, roomCount;

        readonly int[,] zone;
        readonly Dictionary<int, List<Vector2Int>> roomCells = new Dictionary<int, List<Vector2Int>>();
        readonly List<EdgeKey> zoneBoundaries = new List<EdgeKey>();
        static readonly List<Vector2Int> NoCells = new List<Vector2Int>();

        /// <summary>All edges separating two different zones (including the board perimeter).</summary>
        public IReadOnlyList<EdgeKey> ZoneBoundaryEdges { get { return zoneBoundaries; } }

        public BoardModel(string id, int[,] zone, int roomCount)
        {
            this.id = id;
            this.zone = zone;
            this.roomCount = roomCount;
            width = zone.GetLength(0);
            height = zone.GetLength(1);

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    int z = zone[x, y];
                    if (z <= 0) continue;
                    List<Vector2Int> list;
                    if (!roomCells.TryGetValue(z, out list)) roomCells[z] = list = new List<Vector2Int>();
                    list.Add(new Vector2Int(x, y));
                }

            // Horizontal edges (y = 0..height) then vertical edges (x = 0..width)
            for (int x = 0; x < width; x++)
                for (int y = 0; y <= height; y++)
                {
                    var e = new EdgeKey(x, y, true);
                    if (IsZoneBoundary(e)) zoneBoundaries.Add(e);
                }
            for (int x = 0; x <= width; x++)
                for (int y = 0; y < height; y++)
                {
                    var e = new EdgeKey(x, y, false);
                    if (IsZoneBoundary(e)) zoneBoundaries.Add(e);
                }
        }

        public bool InBounds(int x, int y) { return x >= 0 && y >= 0 && x < width && y < height; }

        /// <summary>Zone of the cell, -1 if void or out of bounds.</summary>
        public int ZoneAt(int x, int y) { return InBounds(x, y) ? zone[x, y] : -1; }

        public CellKind KindAt(int x, int y)
        {
            int z = ZoneAt(x, y);
            return z < 0 ? CellKind.Void : (z == 0 ? CellKind.Corridor : CellKind.Room);
        }

        public bool IsWalkable(int x, int y) { return ZoneAt(x, y) >= 0; }

        public IReadOnlyList<Vector2Int> RoomCells(int roomZone)
        {
            List<Vector2Int> l;
            return roomCells.TryGetValue(roomZone, out l) ? (IReadOnlyList<Vector2Int>)l : NoCells;
        }

        /// <summary>Are the zones different on either side of the cell's 'side' edge?</summary>
        public bool IsZoneBoundary(int x, int y, Dir side)
        {
            int nx = x + DirUtil.DX[(int)side], ny = y + DirUtil.DY[(int)side];
            return ZoneAt(x, y) != ZoneAt(nx, ny);
        }

        public bool IsZoneBoundary(EdgeKey e)
        {
            var a = e.CellA; var b = e.CellB;
            return ZoneAt(a.x, a.y) != ZoneAt(b.x, b.y);
        }
    }
}