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

// What the monster AI needs from the board. The Grid Map owner implements this.
public interface IMonsterGrid
{

    bool CanStep(Vector2Int from, Vector2Int to);

    // True if a hero or a monster currently stands on this cell.
    bool IsOccupied(Vector2Int cell);

    Vector3 CellToWorld(Vector2Int cell);

    // Living heroes currently on the board (dead ones must be removed).
    IReadOnlyList<Hero> Heroes { get; }
    Vector2Int GetHeroCell(Hero hero);
}

// Decides what a monster does this turn. It NEVER changes the game state: it only returns a plan.
// A human Zargon can reuse Reach() to highlight legal cells.
//
// Rules respected: fixed movement, orthogonal moves only, cannot cross figures or walls,
// one attack per turn on an adjacent hero, "move then act" or "act then move" (one move at most).
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

        // 1. A hero is already adjacent: attack without moving.
        Hero adjacentHero = null;
        float adjacentScore = float.NegativeInfinity;
        foreach (Hero hero in heroes)
        {
            if (!IsAdjacent(monster.cell, grid.GetHeroCell(hero))) continue;
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
        Reach(grid, monster.cell, monster.movementCells, true, out distance, out cameFrom);

        // 2. Reach a cell next to a hero, then attack.
        if (monster.CanAct)
        {
            Vector2Int bestCell = monster.cell;
            Hero bestHero = null;
            float bestScore = float.NegativeInfinity;
            int bestDistance = int.MaxValue;

            foreach (KeyValuePair<Vector2Int, int> entry in distance)
            {
                if (entry.Key == monster.cell) continue;

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
                plan.Add(MonsterAction.Move(monster, BuildPath(cameFrom, monster.cell, bestCell)));
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

        Vector2Int approachCell = monster.cell;
        int approachDistance = NearestHeroDistance(monster.cell, heroMaps);
        int approachSteps = 0;

        foreach (KeyValuePair<Vector2Int, int> entry in distance)
        {
            if (entry.Key == monster.cell) continue;
            int d = NearestHeroDistance(entry.Key, heroMaps);
            if (d < approachDistance || (d == approachDistance && approachCell != monster.cell && entry.Value < approachSteps))
            {
                approachDistance = d;
                approachCell = entry.Key;
                approachSteps = entry.Value;
            }
        }

        if (approachCell != monster.cell)
            plan.Add(MonsterAction.Move(monster, BuildPath(cameFrom, monster.cell, approachCell)));

        return plan;
    }

    public static bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }

    // BFS from 'start' up to maxSteps cells (start included with distance 0).
    // blockedByFigures = true  -> cannot enter cells with a hero/monster (real movement)
    // blockedByFigures = false -> only walls count (used to measure how far a hero is)
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
                if (blockedByFigures && grid.IsOccupied(next)) continue;

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