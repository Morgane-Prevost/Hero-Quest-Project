using System.Collections.Generic;
using HeroQuest.Board;
using UnityEngine;

// Board rules for the Terror spells, on top of BoardBuilder (zones, cells) and LevelBuilder (walls, doors).
// Create it once: new BoardDreadWorld(board, level). Same pattern as BoardMonsterGrid.
public class BoardDreadWorld : IDreadWorld
{
    private readonly BoardBuilder board;
    private readonly LevelBuilder level;

    public BoardDreadWorld(BoardBuilder board, LevelBuilder level)
    {
        this.board = board;
        this.level = level;
    }

    public bool IsInRoom(Vector2Int cell) => board.Model.ZoneAt(cell.x, cell.y) > 0;

    // Room: all its cells. Corridor: the connected corridor cells (not through walls or closed doors).
    public IEnumerable<Vector2Int> AreaOf(Vector2Int cell)
    {
        int zone = board.Model.ZoneAt(cell.x, cell.y);
        var result = new List<Vector2Int>();
        if (zone < 0) return result;
        if (zone > 0) return board.Model.RoomCells(zone);

        var seen = new HashSet<Vector2Int> { cell };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(cell);
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            result.Add(current);
            for (int i = 0; i < 4; i++)
            {
                var next = new Vector2Int(current.x + DirUtil.DX[i], current.y + DirUtil.DY[i]);
                if (seen.Contains(next) || board.Model.ZoneAt(next.x, next.y) != 0 || !CanStep(current, next)) continue;
                seen.Add(next);
                queue.Enqueue(next);
            }
        }
        return result;
    }

    // Straight line (8 directions) until a wall, a closed door, an obstructed cell or the board edge.
    public IEnumerable<Vector2Int> LineFrom(Vector2Int start, Vector2Int direction)
    {
        var cells = new List<Vector2Int>();
        Vector2Int current = start;
        while (true)
        {
            Vector2Int next = current + direction;
            if (!CanStep(current, next)) break;
            cells.Add(next);
            current = next;
        }
        return cells;
    }

    // Nearest first (Chebyshev distance), then x, then y: always the same order.
    public IEnumerable<Vector2Int> FreeCellsAround(Vector2Int cell)
    {
        var free = new List<Vector2Int>();
        foreach (Vector2Int c in AreaOf(cell)) if (IsFree(c)) free.Add(c);
        free.Sort((a, b) =>
        {
            int da = Mathf.Max(Mathf.Abs(a.x - cell.x), Mathf.Abs(a.y - cell.y));
            int db = Mathf.Max(Mathf.Abs(b.x - cell.x), Mathf.Abs(b.y - cell.y));
            if (da != db) return da.CompareTo(db);
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return a.y.CompareTo(b.y);
        });
        return free;
    }

    public bool IsFree(Vector2Int c)
    {
        CellView view = board.GetCell(c.x, c.y);
        return view != null && view.IsFree && !level.Model.IsBlocked(c.x, c.y);
    }

    public IDamageable VictimAt(Vector2Int c)
    {
        GridPawn pawn = PawnAt(c);
        if (pawn == null) return null;
        IDamageable victim = pawn.GetComponent<IDamageable>();
        return victim;
    }

    public IDreadHeroTarget HeroAt(Vector2Int c)
    {
        GridPawn pawn = PawnAt(c);
        if (pawn == null) return null;
        Hero hero = pawn.GetComponent<Hero>();
        return hero != null ? hero : null;
    }

    public Monster Spawn(MonsterData data, Vector2Int cell) => MonsterManager.Instance.CreateMonster(data, cell);

    public bool Teleport(Monster monster, Vector2Int cell)
        => monster != null && monster.Pawn != null && IsFree(cell) && monster.Pawn.PlaceAt(cell);


    private GridPawn PawnAt(Vector2Int c)
    {
        CellView view = board.GetCell(c.x, c.y);
        return view != null ? view.Occupant : null;
    }


    // because a diagonal line only touches wall corners (rulebook p.14: touching a corner does not block).
    private bool CanStep(Vector2Int a, Vector2Int b)
    {
        if (!board.Model.IsWalkable(b.x, b.y) || level.Model.IsBlocked(b.x, b.y)) return false;
        bool diagonal = a.x != b.x && a.y != b.y;
        return diagonal || !EdgeBlocked(a, b);
    }

    // Wall, or door that is not open (a secret door not found yet counts as closed).
    private bool EdgeBlocked(Vector2Int a, Vector2Int b)
    {
        for (int i = 0; i < 4; i++)
        {
            if (a.x + DirUtil.DX[i] != b.x || a.y + DirUtil.DY[i] != b.y) continue;

            EdgeKey edge = EdgeKey.FromCell(a.x, a.y, (Dir)i);
            ElementDef door = level.Model.DoorAt(edge);
            if (door != null)
            {
                PlacedElement placed = level.GetElement(door.id);
                return placed == null || !placed.IsOpen;
            }
            return level.Model.HasWall(edge);
        }
        return true;   // not neighbours
    }
}