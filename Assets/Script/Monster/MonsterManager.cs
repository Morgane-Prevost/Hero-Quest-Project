using UnityEngine;
using System.Collections.Generic;

public class MonsterManager : MonoBehaviour
{
    [SerializeField] private List<MonsterData> monsters;
    [SerializeField] private List<GameObject> aliveMonsters;

    public static MonsterManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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

    public List<GameObject> GetAliveMonsters()
    {
        return aliveMonsters;
    }
}