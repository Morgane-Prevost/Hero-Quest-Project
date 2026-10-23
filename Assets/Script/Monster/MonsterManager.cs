using UnityEngine;
using System.Collections.Generic;

public class MonsterManager : MonoBehaviour
{
    [SerializeField] private List<MonsterData> monsters;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateMonster(MonsterData monsterData, string name)
    {
        // Create a new monster instance
        var monster = new Monster();
        monster.Initialize(monsterData, name);
    }
}