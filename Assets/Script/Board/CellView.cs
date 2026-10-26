using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>
    /// A cell on the board. One cell = ONE single pawn position: its center.
    /// </summary>
    public class CellView : MonoBehaviour
    {
        public int X, Y, Zone;
        public CellKind Kind;
        public Renderer FloorRenderer;

        public Vector2Int Coord { get { return new Vector2Int(X, Y); } }

        /// <summary>Center of the cell, at ground level (world). The only anchor point for a pawn.</summary>
        public Vector3 Center { get { return transform.position; } }

        /// <summary>Pawn currently on the cell (null if free).</summary>
        public GridPawn Occupant { get; private set; }
        public bool IsFree { get { return Occupant == null; } }

        public void Init(int x, int y, int zone, CellKind kind, Renderer r)
        { X = x; Y = y; Zone = zone; Kind = kind; FloorRenderer = r; }

        internal bool TryOccupy(GridPawn p)
        {
            if (Occupant != null && Occupant != p) return false;
            Occupant = p;
            return true;
        }

        internal void Release(GridPawn p) { if (Occupant == p) Occupant = null; }
    }
}