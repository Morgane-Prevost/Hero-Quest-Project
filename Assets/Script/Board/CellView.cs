using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>
    /// One board cell. A cell has exactly ONE pawn anchor: its center.
    /// </summary>
    public class CellView : MonoBehaviour
    {
        public int X, Y, Zone;
        public CellKind Kind;
        public Renderer FloorRenderer;

        public Vector2Int Coord { get { return new Vector2Int(X, Y); } }

        /// <summary>Cell center at floor level (world space). The only allowed anchor for a pawn.</summary>
        public Vector3 Center { get { return transform.position; } }

        /// <summary>Pawn currently standing on the cell (null if free).</summary>
        public GridPawn Occupant { get; private set; }
        public bool IsFree { get { return Occupant == null; } }

        /// <summary>False when the cell belongs to a room the current level does not use.</summary>
        public bool Playable { get; private set; }

        public void Init(int x, int y, int zone, CellKind kind, Renderer r)
        { X = x; Y = y; Zone = zone; Kind = kind; FloorRenderer = r; Playable = true; }

        /// <summary>Shows/hides the floor and its colliders. A hidden cell refuses pawns and mouse picking.</summary>
        public void SetPlayable(bool playable)
        {
            Playable = playable;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = playable;
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = playable;
        }

        internal bool TryOccupy(GridPawn p)
        {
            if (Occupant != null && Occupant != p) return false;
            Occupant = p;
            return true;
        }

        internal void Release(GridPawn p) { if (Occupant == p) Occupant = null; }
    }
}