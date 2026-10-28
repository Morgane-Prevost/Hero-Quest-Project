using HeroQuest.Board;
using System;
using UnityEngine;

public class Monster : MonoBehaviour, ISpellMonsterTarget
{
    public string monsterName;

    public int healthPoints;
    public int mindPoints;
    public int attackDices;
    public int defenseDices;
    public int movementCells;

    // Turn state: enforces "move + action" or "action + move", never move-action-move.
    public bool hasMoved;
    public bool hasActed;

    private bool skipNextTurn;
    public bool IsAsleep {  get; private set; }

    private GridPawn pawn;

    // Identity and families (from MonsterData). Tags are used by combat/spell rules, e.g. "undead".
    private string[] tags = Array.Empty<string>();
    public string Id { get; private set; }
    public string[] Tags => tags;   // string[] so it can be copied into AttackContext.targetTags
    public bool HasTag(string tag) => Array.IndexOf(tags, tag) >= 0;

    // The GridPawn is what puts the monster on the board (and what Hero / MovementSystem look at).
    public GridPawn Pawn
    {
        get
        {
            if (pawn == null) pawn = GetComponent<GridPawn>();
            return pawn;
        }
    }

    // Logical cell of the monster = cell of its GridPawn (single source of truth, no copy to keep in sync).
    public Vector2Int cell
    {
        get { return (Pawn != null && Pawn.HasCell) ? Pawn.Cell : new Vector2Int(-1, -1); }
    }

    public bool IsDead => healthPoints <= 0;
    public bool CanMove => !hasMoved && !IsAsleep;
    public bool CanAct => !hasActed && !IsAsleep;

    public event Action<Monster> Died;

    //To add when classes are implemented

    public void Initialize(MonsterData monsterData, string name)
    {
        monsterName = name;
        Id = monsterData.id;
        tags = monsterData.tags != null ? (string[])monsterData.tags.Clone() : Array.Empty<string>();
        healthPoints = monsterData.healthPoints;
        mindPoints = monsterData.mindPoints;
        attackDices = monsterData.attackDices;
        defenseDices = monsterData.defenseDices;
        movementCells = monsterData.movementCells;
    }

    public void StartTurn()
    {
        hasMoved = false;
        hasActed = false;

        if (skipNextTurn)   // tornado: the monster passes this turn
        {
            hasMoved = true;
            hasActed = true;
            skipNextTurn = false;
        }

    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead) return;
        healthPoints = Mathf.Max(0, healthPoints - amount);
        if (IsDead) Died?.Invoke(this);
    }


    // NEW SPELL METHODS

    public void SkipNextTurn() => skipNextTurn = true;
    public void FallAsleep() => IsAsleep = true;

    // One red die per mind point, a 6 breaks the spell (card text).
    public bool TryBreakSleep(IRandomPicker picker)
    {
        if (!IsAsleep) return true;
        for (int i = 0; i < mindPoints; i++)
        {
            if (picker.Pick(6) + 1 == 6)
            {
                IsAsleep = false;
                return true;
            }
        }
        return false;
    }
}