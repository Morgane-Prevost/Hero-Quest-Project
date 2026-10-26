using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Runs Zargon's AI turn and applies each MonsterAction:
// every monster on the board plans (MonsterPlanner), then moves / attacks one after the other.
// Call StartMonsterTurn() when all heroes have played.
//
// Damage is NOT applied to the hero here (I don't know the Hero's HP API):
// the hero side subscribes to AttackResolved and applies 'damage'.
public class MonsterActionExecutor : MonoBehaviour
{
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private ActionManager actionManager;
    [SerializeField] private float moveSpeed = 6f;

    private IMonsterGrid grid;
    private Func<Hero, float> targetScore;

    // monster, hero, damage the hero must lose (already includes the hero's defense roll)
    public event Action<Monster, Hero, int> AttackResolved;
    public event Action TurnFinished;

    public void Setup(IMonsterGrid grid, Func<Hero, float> targetScore = null)
    {
        this.grid = grid;
        this.targetScore = targetScore;
    }

    public void StartMonsterTurn()
    {
        StartCoroutine(RunTurn());
    }

    private IEnumerator RunTurn()
    {
        // monsters can die (and leave the manager) during the turn.
        List<Monster> snapshot = new List<Monster>(monsterManager.ActiveMonsters);

        foreach (Monster monster in snapshot)
        {
            if (monster == null || monster.IsDead) continue;

            monster.StartTurn();
            List<MonsterAction> plan = MonsterPlanner.PlanTurn(monster, grid, targetScore);

            foreach (MonsterAction action in plan)
                yield return Execute(action);
        }

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
        if (!monster.CanMove)
        {
            Debug.LogError(monster.monsterName + " already moved this turn (rule violation).");
            yield break;
        }
        if (action.path.Count > monster.movementCells)
        {
            Debug.LogError(monster.monsterName + " path longer than its movement (rule violation).");
            yield break;
        }

        monster.hasMoved = true;
        foreach (Vector2Int step in action.path)
        {
            Vector3 target = grid.CellToWorld(step);
            while (monster != null && (monster.transform.position - target).sqrMagnitude > 0.0001f)
            {
                monster.transform.position = Vector3.MoveTowards(monster.transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }
            if (monster == null) yield break;
            monster.cell = step;
        }
    }

    private void DoAttack(MonsterAction action)
    {
        Monster monster = action.monster;
        Hero hero = action.target;

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

        // Your friend's existing combat: monster attacks, hero defends.
        int damage = actionManager.Fight(hero, monster, false);
        AttackResolved?.Invoke(monster, hero, damage);
    }
}