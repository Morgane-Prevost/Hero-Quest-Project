using HeroQuest.Board;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    [SerializeField] private BoardBuilder board;
    [SerializeField] private List<MonsterData> monsters;

    public static MonsterManager Instance { get; private set; }

    // Quest hooks: HeroQuestWorldBridge listens to these.
    public event System.Action<Monster> MonsterSpawned;   // after creation (level build or SpawnByType)
    public event System.Action<Monster> MonsterKilled;    // just before the monster is destroyed (cell still valid)

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

    // Living monsters on the board, revealed or not (check Monster.revealedToHeroes).
    private readonly List<Monster> activeMonsters = new List<Monster>();
    public IReadOnlyList<Monster> ActiveMonsters => activeMonsters;

    public MonsterData GetData(string monsterName)
    {
        return monsters.Find(m => m.monsterName == monsterName);
    }

    public MonsterData GetDataById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return monsters.Find(m => !string.IsNullOrEmpty(m.id) &&
                                  string.Equals(m.id, id.Trim(), System.StringComparison.OrdinalIgnoreCase));
    }

    public MonsterData FindData(string typeName)
    {
        // 1. exact id ("goblin"), 2. loose match on the name
        MonsterData byId = GetDataById(typeName);
        if (byId != null) return byId;

        string wanted = Normalize(typeName);
        return monsters.Find(m => Normalize(m.monsterName) == wanted);
    }

    private static string Normalize(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        var sb = new System.Text.StringBuilder();
        foreach (char c in name) if (char.IsLetter(c)) sb.Append(char.ToLowerInvariant(c));
        string s = sb.ToString();
        if (s.EndsWith("ies")) s = s.Substring(0, s.Length - 3) + "y";
        else if (s.EndsWith("s")) s = s.Substring(0, s.Length - 1);
        return s;
    }

    // Kept for existing callers: the position is converted to a board cell.
    public Monster CreateMonster(MonsterData data, Vector3 position)
    {
        Vector2Int cell;
        if (board == null || !board.WorldToCell(position, out cell))
        {
            Debug.LogError("[Monster] Position " + position + " is not on the board.");
            return null;
        }
        return CreateMonster(data, cell);
    }

    // Creates the monster ON the board: GridPawn on the cell center (occupies the cell, no sharing).
    public Monster CreateMonster(MonsterData data, Vector2Int cell)
    {
        if (board == null)
        {
            Debug.LogError("[Monster] MonsterManager has no BoardBuilder assigned.");
            return null;
        }

        CellView target = board.GetCell(cell.x, cell.y);
        if (target == null) { Debug.LogError("[Monster] Cell " + cell + " does not exist."); return null; }
        if (!target.IsFree) { Debug.LogWarning("[Monster] Cell " + cell + " is already occupied."); return null; }

        GameObject monsterObj = Instantiate(data.prefab, board.CellToWorld(cell.x, cell.y), Quaternion.identity);

        if (!monsterObj.TryGetComponent(out GridPawn pawn))
            pawn = monsterObj.AddComponent<GridPawn>();
        pawn.board = board;

        if (!monsterObj.TryGetComponent(out Monster monster))
            monster = monsterObj.AddComponent<Monster>();

        monster.Initialize(data, data.monsterName);
        monster.monsterType = data.monsterName;   // used by kill bounties / quest conditions

        if (!pawn.PlaceAt(cell))
        {
            Destroy(monsterObj);
            return null;
        }

        monster.Died += OnMonsterDied;
        activeMonsters.Add(monster);
        return monster;
    }

    // Called by LevelBuilder for every "monsters" entry of the level JSON (replaces the old red placeholder).
    // The monster exists from the start; LevelBuilder hides it from the heroes until it is revealed.
    public Monster SpawnForElement(string elementId, string typeName, Vector2Int cell)
    {
        MonsterData data = FindData(typeName);
        if (data == null)
        {
            Debug.LogWarning("[Monster] No MonsterData for level type '" + typeName + "' (element " + elementId + ").");
            return null;
        }
        Monster monster = CreateMonster(data, cell);
        if (monster == null) return null;
        monster.elementId = elementId;
        monster.revealedToHeroes = false;
        Debug.Log("[Monster] Spawned " + elementId + " (" + data.monsterName + ") at " + cell);
        MonsterSpawned?.Invoke(monster);
        return monster;
    }

    // Spawns a monster by type name (wandering monster, quest spawn). No level element behind it.
    public Monster SpawnByType(string typeName, Vector2Int cell)
    {
        MonsterData data = FindData(typeName);
        if (data == null)
        {
            Debug.LogWarning("[Monster] No MonsterData for type '" + typeName + "'.");
            return null;
        }
        Monster monster = CreateMonster(data, cell);
        if (monster == null) return null;
        monster.elementId = "";
        MonsterSpawned?.Invoke(monster);
        return monster;
    }

    // Called by LevelBuilder.Clear() before a new level is built.
    public void ClearMonsters()
    {
        foreach (Monster m in new List<Monster>(activeMonsters))
        {
            if (m == null) continue;
            m.Died -= OnMonsterDied;
            if (m.Pawn != null) m.Pawn.Remove();   // frees the cell NOW (Destroy only happens at end of frame)
            m.gameObject.SetActive(false);
            Destroy(m.gameObject);
        }
        activeMonsters.Clear();
    }

    private void OnMonsterDied(Monster monster)
    {
        monster.Died -= OnMonsterDied;
        MonsterKilled?.Invoke(monster);
        activeMonsters.Remove(monster);
        if (monster.Pawn != null) monster.Pawn.Remove();
        Destroy(monster.gameObject);
    }
}
