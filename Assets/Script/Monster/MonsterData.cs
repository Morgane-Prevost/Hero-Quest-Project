using UnityEngine;

[CreateAssetMenu(fileName = "MonsterData", menuName = "GameDatas/MonsterData")]
public class MonsterData : ScriptableObject
{
    public GameObject prefab;
    public Texture2D texture;

    public string monsterName;

    public int healthPoints;
    public int mindPoints;
    public int attackDices;
    public int defenseDices;

    public int movementDices;

    //To add when classes are implemented

}