using UnityEngine;
using System.Collections.Generic;

public class MonsterManager : MonoBehaviour
{
    [SerializeField] private List<MonsterData> monsters;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CreateMonster(monsters[0], Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public MonsterData GetData(string monsterName)
    {
        return monsters.Find(m => m.monsterName == monsterName);
    }
    
    public Monster CreateMonster(MonsterData data, Vector3 position)
    {
        GameObject monsterObj = Instantiate(data.prefab, position, Quaternion.identity);

        if (!monsterObj.TryGetComponent(out Monster monster))
            monster = monsterObj.AddComponent<Monster>();

        monster.Initialize(data, data.monsterName);
        return monster;
    }
}