using UnityEngine;
using System.Collections.Generic;

public class MonsterManager : MonoBehaviour
{
    [SerializeField] private List<MonsterData> monsters;

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