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

    // Turn state: enforces "move + action" or "action + move".
    public bool hasMoved;
    public bool hasActed;

    private GridPawn pawn;

    // The GridPawn is what puts the monster on the board (and what Hero / MovementSystem look at).
    public GridPawn Pawn
    {
        get
        {
            if (pawn == null) pawn = GetComponent<GridPawn>();
            return pawn;
        }
    }

    // Logical cell of the monster = cell of its GridPawn .
    public Vector2Int cell
    {
        get { return (Pawn != null && Pawn.HasCell) ? Pawn.Cell : new Vector2Int(-1, -1); }
    }

    public bool IsDead => healthPoints <= 0;
    public bool CanMove => !hasMoved;
    public bool CanAct => !hasActed;

    public event Action<Monster> Died;


    public void Initialize(MonsterData monsterData, string name)
    {
        monsterName = name;
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
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead) return;
        healthPoints = Mathf.Max(0, healthPoints - amount);
        if (IsDead) Died?.Invoke(this);
    }
}