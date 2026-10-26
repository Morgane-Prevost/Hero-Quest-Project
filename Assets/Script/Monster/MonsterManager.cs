//using UnityEngine;
//using System.Collections.Generic;

//public class MonsterManager : MonoBehaviour
//{
//    [SerializeField] private List<MonsterData> monsters;

//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {
//    }

//    // Update is called once per frame
//    void Update()
//    {

//    }

//    public MonsterData GetData(string monsterName)
//    {
//        return monsters.Find(m => m.monsterName == monsterName);
//    }

//    public Monster CreateMonster(MonsterData data, Vector3 position)
//    {
//        GameObject monsterObj = Instantiate(data.prefab, position, Quaternion.identity);

//        if (!monsterObj.TryGetComponent(out Monster monster))
//            monster = monsterObj.AddComponent<Monster>();

//        monster.Initialize(data, data.monsterName);
//        return monster;
//    }
//}

using UnityEngine;
using System.Collections.Generic;

public class MonsterManager : MonoBehaviour
{
    [SerializeField] private List<MonsterData> monsters;

    // Living monsters on the board (Zargon's turn moves all of them).
    private readonly List<Monster> activeMonsters = new List<Monster>();
    public IReadOnlyList<Monster> ActiveMonsters => activeMonsters;

    public MonsterData GetData(string monsterName)
    {
        return monsters.Find(m => m.monsterName == monsterName);
    }

    // Set monster.cell after creating it.
    public Monster CreateMonster(MonsterData data, Vector3 position)
    {
        GameObject monsterObj = Instantiate(data.prefab, position, Quaternion.identity);

        if (!monsterObj.TryGetComponent(out Monster monster))
            monster = monsterObj.AddComponent<Monster>();

        monster.Initialize(data, data.monsterName);
        monster.Died += OnMonsterDied;
        activeMonsters.Add(monster);
        return monster;
    }

    private void OnMonsterDied(Monster monster)
    {
        monster.Died -= OnMonsterDied;
        activeMonsters.Remove(monster);
        Destroy(monster.gameObject);
    }
}