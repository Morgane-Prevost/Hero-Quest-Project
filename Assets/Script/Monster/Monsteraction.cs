using HeroQuest.Board;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum MonsterActionType
{
    Move,
    Attack,
    CastSpell // reserved for the Dread Sorcerer (spells depend on the quest)
}

public class MonsterAction
{
    public MonsterActionType type;
    public Monster monster;
    public List<Vector2Int> path; // Move: cells to walk through, start excluded
    public Hero target;           // Attack / CastSpell

    public static MonsterAction Move(Monster monster, List<Vector2Int> path)
    {
        return new MonsterAction { type = MonsterActionType.Move, monster = monster, path = path };
    }

    public static MonsterAction Attack(Monster monster, Hero target)
    {
        return new MonsterAction { type = MonsterActionType.Attack, monster = monster, target = target };
    }
}

// What the monster AI needs from the board.
public interface IMonsterGrid
{
    // Can a figure step from 'from' to the orthogonally adjacent cell 'to'?
    // False for: void / out of board, walls, closed doors, obstructed cells, furniture.
    bool CanStep(Vector2Int from, Vector2Int to);

    // A hero or a monster stands on this cell: a monster cannot STOP here.
    bool IsOccupied(Vector2Int cell);

    // Can a monster walk THROUGH this cell? (free cell, or a cell with another monster). Never through a hero.
    bool CanTraverse(Vector2Int cell);

    Vector3 CellToWorld(Vector2Int cell);

    // Living heroes currently on the board.
    IReadOnlyList<Hero> Heroes { get; }
    Vector2Int GetHeroCell(Hero hero);
}

// Real implementation on top of the board / level code (BoardBuilder, LevelBuilder, GridPawn).
//
// Rules applied:
// - no diagonal move, no wall, no obstructed cell, no furniture
// - monsters NEVER open doors: a door blocks until a hero has opened it (PlacedElement.IsOpen)
// - cannot pass through a hero, cannot share a cell
// - monsters ignore traps
public class BoardMonsterGrid : IMonsterGrid
{
    private readonly BoardBuilder board;
    private readonly LevelBuilder level;
    private readonly bool canPassThroughMonsters;
    private readonly Dictionary<Hero, Vector2Int> heroCells = new Dictionary<Hero, Vector2Int>();
    
    public BoardMonsterGrid(BoardBuilder board, LevelBuilder level, bool canPassThroughMonsters = true)
    {
        this.board = board;
        this.level = level;
        this.canPassThroughMonsters = canPassThroughMonsters;
    }

    public bool CanStep(Vector2Int from, Vector2Int to)
    {
        if (board == null || board.Model == null) return false;
        if (!MonsterPlanner.IsAdjacent(from, to)) return false;
        if (!board.Model.IsWalkable(to.x, to.y)) return false;

        LevelModel model = level != null ? level.Model : null;
        if (model == null) return true; // no level loaded: floor only

        EdgeKey edge;
        if (!EdgeKey.TryBetween(from, to, out edge)) return false;

        ElementDef door = model.DoorAt(edge);
        if (door != null)
        {
            // Closed or secret door = wall for monsters (they cannot open doors).
            PlacedElement placed = level.GetElement(door.id);
            if (placed == null || !placed.IsOpen) return false;
        }
        else if (model.HasWall(edge) && !level.IsEdgeOpen(edge))
        {
            return false;
        }

        if (model.IsBlocked(to.x, to.y)) return false; // obstructed cell or furniture
        return true;
    }

    public bool IsOccupied(Vector2Int cell)
    {
        CellView view = board.GetCell(cell.x, cell.y);
        return view != null && view.Occupant != null;
    }

    public bool CanTraverse(Vector2Int cell)
    {
        CellView view = board.GetCell(cell.x, cell.y);
        if (view == null) return false;
        if (view.Occupant == null) return true;
        return canPassThroughMonsters && view.Occupant.GetComponent<Monster>() != null;
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return board.CellToWorld(cell.x, cell.y);
    }

    public IReadOnlyList<Hero> Heroes
    {
        get
        {
            // Rebuilt each time the planner asks (once per monster turn), cells are cached for the planning.
            heroCells.Clear();
            List<Hero> result = new List<Hero>();
#if UNITY_2023_1_OR_NEWER
            Hero[] all = UnityEngine.Object.FindObjectsByType<Hero>(FindObjectsSortMode.None);
#else
            Hero[] all = UnityEngine.Object.FindObjectsOfType<Hero>();
#endif
            foreach (Hero hero in all)
            {
                if (hero.currentBodyPoints <= 0) continue; // dead hero
                GridPawn pawn = hero.GetComponent<GridPawn>();
                if (pawn == null || !pawn.HasCell) continue;
                result.Add(hero);
                heroCells[hero] = pawn.Cell;
            }
            return result;
        }
    }

    public Vector2Int GetHeroCell(Hero hero)
    {
        Vector2Int cell;
        if (heroCells.TryGetValue(hero, out cell)) return cell;
        return hero.GetComponent<GridPawn>().Cell;
    }
}

public static class MonsterPlanner
{
    // Orthogonal only: monsters never move diagonally.
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    // targetScore: higher = more attractive target (e.g. low HP). Null = all heroes equal.
    public static List<MonsterAction> PlanTurn(Monster monster, IMonsterGrid grid, Func<Hero, float> targetScore = null)
    {
        Func<Hero, float> score = targetScore ?? (h => 0f);
        List<MonsterAction> plan = new List<MonsterAction>();
        IReadOnlyList<Hero> heroes = grid.Heroes;
        if (heroes == null || heroes.Count == 0) return plan;

        Vector2Int start = monster.cell;

        // 1. A hero is already adjacent: attack without moving.
        Hero adjacentHero = null;
        float adjacentScore = float.NegativeInfinity;
        foreach (Hero hero in heroes)
        {
            if (!IsAdjacent(start, grid.GetHeroCell(hero))) continue;
            float s = score(hero);
            if (s > adjacentScore) { adjacentScore = s; adjacentHero = hero; }
        }
        if (adjacentHero != null)
        {
            if (monster.CanAct) plan.Add(MonsterAction.Attack(monster, adjacentHero));
            return plan;
        }

        if (!monster.CanMove) return plan;

        Dictionary<Vector2Int, int> distance;
        Dictionary<Vector2Int, Vector2Int> cameFrom;
        Reach(grid, start, monster.movementCells, true, out distance, out cameFrom);

        // 2. Reach a free cell next to a hero, then attack.
        if (monster.CanAct)
        {
            Vector2Int bestCell = start;
            Hero bestHero = null;
            float bestScore = float.NegativeInfinity;
            int bestDistance = int.MaxValue;

            foreach (KeyValuePair<Vector2Int, int> entry in distance)
            {
                if (entry.Key == start) continue;
                if (grid.IsOccupied(entry.Key)) continue; // can walk through, cannot stop

                foreach (Hero hero in heroes)
                {
                    if (!IsAdjacent(entry.Key, grid.GetHeroCell(hero))) continue;

                    float s = score(hero);
                    if (s > bestScore || (s == bestScore && entry.Value < bestDistance))
                    {
                        bestScore = s;
                        bestDistance = entry.Value;
                        bestCell = entry.Key;
                        bestHero = hero;
                    }
                }
            }

            if (bestHero != null)
            {
                plan.Add(MonsterAction.Move(monster, BuildPath(cameFrom, start, bestCell)));
                plan.Add(MonsterAction.Attack(monster, bestHero));
                return plan;
            }
        }

        // 3. Nobody reachable: get as close as possible to the nearest hero (real path distance, walls only).
        List<Dictionary<Vector2Int, int>> heroMaps = new List<Dictionary<Vector2Int, int>>();
        foreach (Hero hero in heroes)
        {
            Dictionary<Vector2Int, int> map;
            Dictionary<Vector2Int, Vector2Int> unused;
            Reach(grid, grid.GetHeroCell(hero), int.MaxValue, false, out map, out unused);
            heroMaps.Add(map);
        }

        Vector2Int approachCell = start;
        int approachDistance = NearestHeroDistance(start, heroMaps);
        int approachSteps = 0;

        foreach (KeyValuePair<Vector2Int, int> entry in distance)
        {
            if (entry.Key == start) continue;
            if (grid.IsOccupied(entry.Key)) continue; // cannot stop here

            int d = NearestHeroDistance(entry.Key, heroMaps);
            if (d < approachDistance || (d == approachDistance && approachCell != start && entry.Value < approachSteps))
            {
                approachDistance = d;
                approachCell = entry.Key;
                approachSteps = entry.Value;
            }
        }

        if (approachCell != start)
            plan.Add(MonsterAction.Move(monster, BuildPath(cameFrom, start, approachCell)));

        return plan;
    }

    public static bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }

    // BFS from 'start' up to maxSteps cells (start included with distance 0).
    // blockedByFigures = true  -> real monster movement: heroes block, other monsters can be crossed
    //                             (the returned cells may include occupied ones: the caller must not stop there)
    // blockedByFigures = false -> only walls / furniture count (used to measure how far a hero is)
    public static void Reach(IMonsterGrid grid, Vector2Int start, int maxSteps, bool blockedByFigures,
        out Dictionary<Vector2Int, int> distance, out Dictionary<Vector2Int, Vector2Int> cameFrom)
    {
        distance = new Dictionary<Vector2Int, int>();
        cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        distance[start] = 0;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int currentDistance = distance[current];
            if (currentDistance >= maxSteps) continue;

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = current + dir;
                if (distance.ContainsKey(next)) continue;
                if (!grid.CanStep(current, next)) continue;
                if (blockedByFigures && !grid.CanTraverse(next)) continue;

                distance[next] = currentDistance + 1;
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }
    }

    // Path from start (excluded) to target (included).
    private static List<Vector2Int> BuildPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int target)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = target;
        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }

    private static int NearestHeroDistance(Vector2Int cell, List<Dictionary<Vector2Int, int>> maps)
    {
        int best = int.MaxValue;
        foreach (Dictionary<Vector2Int, int> map in maps)
            if (map.TryGetValue(cell, out int d) && d < best) best = d;
        return best;
    }
}