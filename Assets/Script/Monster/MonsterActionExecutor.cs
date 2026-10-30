using HeroQuest.Board;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Runs Zargon's AI turn and applies each MonsterAction:
// every monster on the board plans (MonsterPlanner), then moves / attacks one after the other.
// Call StartMonsterTurn() when all heroes have played.

public class MonsterActionExecutor : MonoBehaviour
{
    [SerializeField] private BoardBuilder board;
    [SerializeField] private LevelBuilder level;
    [SerializeField] private float moveSpeed = 6f;
    [Tooltip("Rulebook does not forbid walking THROUGH another monster (stopping on it is forbidden). To confirm with the client.")]
    [SerializeField] private bool canPassThroughMonsters = true;

    private IMonsterGrid grid;
    private Func<Hero, float> targetScore;
    private bool isRunning;

    public bool IsRunning => isRunning;

    public event Action<Monster, Hero, int> AttackResolved;
    public event Action TurnFinished;

    private void Start()
    {
        if (grid == null) Setup(new BoardMonsterGrid(board, level, canPassThroughMonsters));
    }

    public void Setup(IMonsterGrid grid, Func<Hero, float> targetScore = null)
    {
        this.grid = grid;
        this.targetScore = targetScore;
    }

    public void StartMonsterTurn()
    {
        if (isRunning) { Debug.LogWarning("[Monster] Zargon's turn is already running."); return; }
        if (grid == null) Setup(new BoardMonsterGrid(board, level, canPassThroughMonsters));
        StartCoroutine(RunTurn());
    }

    private IEnumerator RunTurn()
    {
        isRunning = true;

        // monsters can die (and leave the manager) during the turn.
        List<Monster> snapshot = new List<Monster>(MonsterManager.Instance.ActiveMonsters);

        foreach (Monster monster in snapshot)
        {
            if (monster == null || monster.IsDead) continue;
            if (!monster.revealedToHeroes) continue;   // not on the board yet
            if (monster.isDormant) continue;
            
            monster.StartTurn();
            List<MonsterAction> plan = MonsterPlanner.PlanTurn(monster, grid, targetScore);

            foreach (MonsterAction action in plan)
            {
                if (monster == null || monster.IsDead) break;
                yield return Execute(action);
            }
        }

        isRunning = false;
        TurnFinished?.Invoke();
    }

    public IEnumerator Execute(MonsterAction action)
    {
        switch (action.type)
        {
            case MonsterActionType.Move:
                yield return MoveAlong(action);
                break;
            case MonsterActionType.Attack:
                DoAttack(action);
                break;
            default:
                Debug.LogWarning("Monster action not implemented yet: " + action.type);
                break;
        }
    }

    private IEnumerator MoveAlong(MonsterAction action)
    {
        Monster monster = action.monster;
        GridPawn pawn = monster.Pawn;

        if (pawn == null || !pawn.HasCell)
        {
            Debug.LogError(monster.monsterName + " has no GridPawn / is not on the board.");
            yield break;
        }
        if (!monster.CanMove)
        {
            Debug.LogError(monster.monsterName + " already moved this turn (rule violation).");
            yield break;
        }
        if (action.path == null || action.path.Count == 0) yield break;
        if (action.path.Count > monster.movementCells)
        {
            Debug.LogError(monster.monsterName + " path longer than its movement (rule violation).");
            yield break;
        }

        // Re-check every step with the board rules .
        Vector2Int start = monster.cell;
        Vector2Int previous = start;
        foreach (Vector2Int step in action.path)
        {
            if (!grid.CanStep(previous, step) || !grid.CanTraverse(step))
            {
                Debug.LogError(monster.monsterName + " illegal step " + previous + " -> " + step + " (rule violation).");
                yield break;
            }
            previous = step;
        }

        Vector2Int destination = action.path[action.path.Count - 1];
        if (grid.IsOccupied(destination))
        {
            Debug.LogError(monster.monsterName + " cannot stop on occupied cell " + destination + " (rule violation).");
            yield break;
        }

        monster.hasMoved = true;
        monster.OnActed();

        // Animate through the cell centers. The cell is only taken at the end (PlaceAt):
        // the monster may walk through cells occupied by other monsters.
        foreach (Vector2Int step in action.path)
        {
            Vector3 target = grid.CellToWorld(step) + Vector3.up * pawn.heightOffset;

            Vector3 dir = target - monster.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) monster.transform.rotation = Quaternion.LookRotation(dir);

            while (monster != null && (monster.transform.position - target).sqrMagnitude > 0.0001f)
            {
                monster.transform.position = Vector3.MoveTowards(monster.transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }
            if (monster == null) yield break;
        }

        // Snap on the final cell and update the occupation.
        if (!pawn.PlaceAt(destination))
        {
            Debug.LogError(monster.monsterName + " could not take cell " + destination + ": back to " + start);
            pawn.PlaceAt(start);
        }
    }

    private void DoAttack(MonsterAction action)
    {
        Monster monster = action.monster;
        Hero hero = action.target;

        if (hero == null) return;
        if (!monster.CanAct)
        {
            Debug.LogError(monster.monsterName + " already acted this turn (rule violation).");
            return;
        }
        if (!MonsterPlanner.IsAdjacent(monster.cell, grid.GetHeroCell(hero)))
        {
            Debug.LogError(monster.monsterName + " target is not adjacent (rule violation).");
            return;
        }

        monster.hasActed = true;
        monster.OnActed();

        // Combat from ActionManager: monster attacks, hero defends.
        int damage = ActionManager.Instance.Fight(hero, monster, false);
        AttackResolved?.Invoke(monster, hero, damage);
    }
}