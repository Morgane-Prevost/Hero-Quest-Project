using System;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>
    /// A pawn can ONLY be AT THE CENTER OF A CELL. Any placement goes through PlaceAt / SnapToWorld,
    /// which "snaps" the pawn to the center and updates the occupied cell.
    /// </summary>
    [DisallowMultipleComponent]
    public class GridPawn : MonoBehaviour
    {
        public BoardBuilder board;
        [Tooltip("Height above the ground (e.g., base thickness).")]
        public float heightOffset = 0f;
        [Tooltip("Allows multiple pawns on the same cell (disabled = one miniature per cell).")]
        public bool allowSharedCell = false;

        /// <summary>Is the pawn placed on a cell?</summary>
        public bool HasCell { get; private set; }

        /// <summary>LOGICAL CELL of the pawn (reference). Valid only if HasCell.</summary>
        public Vector2Int Cell { get; private set; }

        /// <summary>World center of the occupied cell.</summary>
        public Vector3 CellCenter
        {
            get { return (HasCell && board != null) ? board.CellToWorld(Cell.x, Cell.y) : transform.position; }
        }

        /// <summary>(pawn, old cell [(-1,-1) on first placement], new cell)</summary>
        public event Action<GridPawn, Vector2Int, Vector2Int> CellChanged;

        public bool PlaceAt(Vector2Int c) { return PlaceAt(c.x, c.y); }

        /// <summary>Places the pawn at the center of the cell. False if cell does not exist or is occupied.</summary>
        public bool PlaceAt(int x, int y)
        {
            if (board == null) { Debug.LogError("[GridPawn] 'board' not assigned on " + name); return false; }
            CellView target = board.GetCell(x, y);
            if (target == null) { Debug.LogWarning("[GridPawn] Cell (" + x + "," + y + ") does not exist."); return false; }
            if (!allowSharedCell && !target.IsFree && target.Occupant != this) return false;

            Vector2Int from = HasCell ? Cell : new Vector2Int(-1, -1);
            if (HasCell)
            {
                CellView old = board.GetCell(Cell.x, Cell.y);
                if (old != null) old.Release(this);
            }
            if (!allowSharedCell) target.TryOccupy(this);

            Cell = new Vector2Int(x, y);
            HasCell = true;
            transform.position = target.Center + Vector3.up * heightOffset;
            if (CellChanged != null) CellChanged(this, from, Cell);
            return true;
        }

        /// <summary>Places the pawn on the cell located under a world point (e.g., end of mouse drag).</summary>
        public bool SnapToWorld(Vector3 world)
        {
            Vector2Int c;
            return board != null && board.WorldToCell(world, out c) && PlaceAt(c.x, c.y);
        }

        /// <summary>Cell under the ACTUAL transform position (useful during an animation).</summary>
        public bool TryGetCellFromTransform(out Vector2Int cell)
        {
            cell = default(Vector2Int);
            return board != null && board.WorldToCell(transform.position, out cell);
        }

        /// <summary>Removes the pawn from the grid (death, out of bounds).</summary>
        public void Remove()
        {
            if (HasCell && board != null)
            {
                CellView c = board.GetCell(Cell.x, Cell.y);
                if (c != null) c.Release(this);
            }
            HasCell = false;
        }

        void OnDestroy() { Remove(); }
    }
}