using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "MonsterData", menuName = "GameDatas/MonsterData")]
public class MonsterData : ScriptableObject
{
    public GameObject prefab;
    public Texture2D texture;

    [Tooltip("Unique, lowercase, never renamed once published (e.g. \"goblin\").")]
    public string id;

    [Tooltip("Families, e.g. \"undead\" (empty = none).")]
    public string[] tags;

    public string monsterName;

    public int healthPoints;
    public int mindPoints;
    public int attackDices;
    public int defenseDices;

    // Monsters never roll to move: this is the FIXED maximum number of cells (Goblin 10, Zombie 5...).
    // FormerlySerializedAs keeps the values already typed in your existing assets.
    [FormerlySerializedAs("movementDices")]
    public int movementCells;

}