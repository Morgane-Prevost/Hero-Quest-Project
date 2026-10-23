using UnityEngine;

public class Monster : MonoBehaviour
{
    public string monsterName;

    public int healthPoints;
    public int mindPoints;
    public int attackDices;
    public int defenseDices;

    public int movementDices;

    //To add when classes are implemented


    public void Initialize(MonsterData monsterData, string name)
    {
        monsterName = name;
        healthPoints = monsterData.healthPoints;
        mindPoints = monsterData.mindPoints;
        attackDices = monsterData.attackDices;
        defenseDices = monsterData.defenseDices;
        movementDices = monsterData.movementDices;
    }
}