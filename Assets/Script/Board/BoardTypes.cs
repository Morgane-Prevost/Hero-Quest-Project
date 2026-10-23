using System;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>Directions de grille. N = +Y grille (= +Z monde), E = +X.</summary>
    public enum Dir : byte { N = 0, E = 1, S = 2, W = 3 }

    public enum CellKind : byte { Void = 0, Corridor = 1, Room = 2 }

    public static class DirUtil
    {
        public static readonly int[] DX = { 0, 1, 0, -1 };
        public static readonly int[] DY = { 1, 0, -1, 0 };
        public static Dir Opposite(Dir d) { return (Dir)(((int)d + 2) & 3); }
        public static Vector2Int Offset(Dir d) { return new Vector2Int(DX[(int)d], DY[(int)d]); }
    }

    /// <summary>
    /// Identifiant unique d'une arête entre deux cases (porte, mur, piège de bord...).
    /// horizontal = segment le long de X, à la ligne y, sous la case (x,y) : sépare (x,y-1) et (x,y).
    /// vertical   = segment le long de Z, à la ligne x, à droite de la case (x-1,y) : sépare (x-1,y) et (x,y).
    /// Une même arête a toujours la même clé, quel que soit le côté d'où on la décrit.
    /// </summary>
    public readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        public readonly int x, y;
        public readonly bool horizontal;

        public EdgeKey(int x, int y, bool horizontal) { this.x = x; this.y = y; this.horizontal = horizontal; }

        /// <summary>Arête située sur le côté 'side' de la case (cx,cy).</summary>
        public static EdgeKey FromCell(int cx, int cy, Dir side)
        {
            switch (side)
            {
                case Dir.N: return new EdgeKey(cx, cy + 1, true);
                case Dir.S: return new EdgeKey(cx, cy, true);
                case Dir.E: return new EdgeKey(cx + 1, cy, false);
                default: return new EdgeKey(cx, cy, false);
            }
        }

        /// <summary>Arête entre deux cases adjacentes (false si elles ne sont pas voisines).</summary>
        public static bool TryBetween(Vector2Int a, Vector2Int b, out EdgeKey edge)
        {
            int dx = b.x - a.x, dy = b.y - a.y;
            edge = default(EdgeKey);
            if (dx == 1 && dy == 0) { edge = FromCell(a.x, a.y, Dir.E); return true; }
            if (dx == -1 && dy == 0) { edge = FromCell(a.x, a.y, Dir.W); return true; }
            if (dx == 0 && dy == 1) { edge = FromCell(a.x, a.y, Dir.N); return true; }
            if (dx == 0 && dy == -1) { edge = FromCell(a.x, a.y, Dir.S); return true; }
            return false;
        }

        /// <summary>Case du côté Sud (arête horizontale) ou Ouest (verticale).</summary>
        public Vector2Int CellA { get { return horizontal ? new Vector2Int(x, y - 1) : new Vector2Int(x - 1, y); } }
        /// <summary>Case du côté Nord (arête horizontale) ou Est (verticale).</summary>
        public Vector2Int CellB { get { return new Vector2Int(x, y); } }

        public bool Equals(EdgeKey o) { return x == o.x && y == o.y && horizontal == o.horizontal; }
        public override bool Equals(object obj) { return obj is EdgeKey && Equals((EdgeKey)obj); }
        public override int GetHashCode() { return (x * 31 + y) * 2 + (horizontal ? 1 : 0); }
        public override string ToString() { return (horizontal ? "H(" : "V(") + x + "," + y + ")"; }
    }
}