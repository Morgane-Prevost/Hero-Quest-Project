using HeroQuest.Board;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Computes the cells a hero can reach (movement rules),
/// then delegates highlighting and click handling to the CellPicker.
///
/// Rules:
/// - no diagonal movement (4 orthogonal directions)
/// - cannot cross walls / closed doors
/// - cannot move through monsters
/// - can move through other heroes, but cannot stop on them
/// - no shared cells (except stairs / pit)
/// </summary>
public class MovementSystem : MonoBehaviour
{
    [SerializeField] private BoardBuilder board;
    [SerializeField] private LayerMask obstacleMask;   // walls + closed doors only
    [SerializeField] private float rayHeight = 1f;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public static MovementSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ───────── Public API ─────────

    /// <summary>
    /// Highlights the reachable cells and calls onChosen with the clicked cell.
    /// </summary>
    public void BeginMove(GridPawn hero, int movePoints,
                          Action<Vector2Int> onChosen, Action onCancelled = null)
    {
        List<Vector2Int> cells = GetReachableCells(hero, movePoints);
        CellPicker.Instance.Pick(cells, onChosen, onCancelled);
    }

    /// <summary>
    /// All cells the hero can stop on (BFS).
    /// </summary>
    public List<Vector2Int> GetReachableCells(GridPawn hero, int movePoints)
    {
        var result = new List<Vector2Int>();
        var dist = new Dictionary<Vector2Int, int> { { hero.Cell, 0 } };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(hero.Cell);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int d = dist[current];
            if (d >= movePoints) continue;

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = current + dir;
                if (dist.ContainsKey(next)) continue;

                CellView cell = GetCell(next);
                if (cell == null) continue;                      // outside the board
                if (IsWallBetween(current, next)) continue;      // wall / closed door
                if (IsMonster(cell.Occupant)) continue;          // monster: impassable

                dist[next] = d + 1;
                queue.Enqueue(next);                             // a hero can be crossed

                if (CanStopOn(cell))
                    result.Add(next);
            }
        }

        return result;
    }

    // ───────── Rules ─────────

    private bool CanStopOn(CellView cell)
    {
        if (cell.Occupant == null) return true;   // free cell
        return AllowsSharing(cell);               // occupied: only stairs / pit allowed
    }

    private bool IsWallBetween(Vector2Int a, Vector2Int b)
    {
        Vector3 from = CellToWorld(a) + Vector3.up * rayHeight;
        Vector3 to = CellToWorld(b) + Vector3.up * rayHeight;
        return Physics.Linecast(from, to, obstacleMask, QueryTriggerInteraction.Ignore);
    }
    private CellView GetCell(Vector2Int c) => board.GetCell(c.x, c.y);

    private Vector3 CellToWorld(Vector2Int c) => board.CellToWorld(c.x, c.y);

    private bool IsMonster(GridPawn occupant)
        => occupant != null && occupant.GetComponent<Monster>() != null;

    // stairs / pit (for now, no sharing allowed)
    private bool AllowsSharing(CellView cell)
        => false; // e.g. cell.IsStairs || cell.IsPit
}
