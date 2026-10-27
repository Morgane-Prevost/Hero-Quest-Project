using HeroQuest.Board;
using UnityEngine;

public class Monster : MonoBehaviour
{
    public string monsterName;

    public int healthPoints;
    public int mindPoints;
    public int attackDices;
    public int defenseDices;
    public int movementCells;

    // Position on the board grid (set it when spawning).
    public Vector2Int cell;

    // Turn state: enforces "move + action" or "action + move", never move-action-move.
    public bool hasMoved;
    public bool hasActed;

    public bool IsDead => healthPoints <= 0;
    public bool CanMove => !hasMoved;
    public bool CanAct => !hasActed;

    public event Action<Monster> Died;

    //To add when classes are implemented

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

    public void TakeDamage(int damage)
    {
        healthPoints -= damage;
        if (healthPoints <= 0)
        {
            // Handle monster death logic here
        }
    }
}