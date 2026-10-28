using UnityEngine;
using UnityEngine.Serialization;

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
    
    // FormerlySerializedAs keeps the values already typed in your existing assets.
    [FormerlySerializedAs("movementDices")]
    public int movementCells;

}