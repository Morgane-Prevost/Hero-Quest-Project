using HeroQuest.Board;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    [SerializeField] private BoardBuilder board;
    [Tooltip("Optional: if assigned, level monsters are spawned automatically when the level is built / revealed.")]
    [SerializeField] private LevelBuilder level;
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


    public MonsterData FindData(string typeName)
    {
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

        if (!pawn.PlaceAt(cell))
        {
            Destroy(monsterObj);
            return null;
        }

        monster.Died += OnMonsterDied;
        activeMonsters.Add(monster);
        return monster;
    }

    private LevelBuilder boundLevel;
    private readonly HashSet<string> spawnedElementIds = new HashSet<string>();

    private void Start()
    {
        if (level == null) return;
        level.LevelBuilt += OnLevelBuilt;
        if (level.Model != null) OnLevelBuilt(level); 
    }

    private void OnDestroy()
    {
        if (level != null) level.LevelBuilt -= OnLevelBuilt;
        UnbindLevel();
    }

    private void OnLevelBuilt(LevelBuilder built)
    {
        ClearMonsters();
        SpawnLevelMonsters(built);
    }

    public void ClearMonsters()
    {
        foreach (Monster m in new List<Monster>(activeMonsters))
        {
            if (m == null) continue;
            m.Died -= OnMonsterDied;
            Destroy(m.gameObject);
        }
        activeMonsters.Clear();
    }

    // Rulebook: a monster only exists on the board once a hero has SEEN it (corridor line of sight)
    // or opened its room's door. So real Monster objects are created when LevelBuilder reveals them,
    // not at level start.
    public int SpawnLevelMonsters(LevelBuilder level)
    {
        UnbindLevel();
        if (level == null || level.Model == null) return 0;

        boundLevel = level;
        boundLevel.ElementRevealed += OnElementRevealed;

        int count = 0;
        foreach (ElementDef def in level.Model.Elements)
        {
            if (def.kind != ElementKind.Monster) continue;
            PlacedElement placeholder = level.GetElement(def.id);
            if (placeholder != null && placeholder.VisibleToHeroes && SpawnFromElement(def)) count++;
        }
        return count;
    }

    private void UnbindLevel()
    {
        if (boundLevel != null) boundLevel.ElementRevealed -= OnElementRevealed;
        boundLevel = null;
        spawnedElementIds.Clear();
    }

    private void OnElementRevealed(ElementDef def)
    {
        if (def.kind == ElementKind.Monster) SpawnFromElement(def);
    }

    // Replaces the red placeholder pawn LevelBuilder put for this monster by a real Monster (same cell).
    private bool SpawnFromElement(ElementDef def)
    {
        if (spawnedElementIds.Contains(def.id)) return false;

        MonsterData data = FindData(def.type);
        if (data == null)
        {
            Debug.LogWarning("[Monster] No MonsterData for level type '" + def.type + "' (element " + def.id + ").");
            return false;
        }

        PlacedElement placeholder = boundLevel.GetElement(def.id);
        if (placeholder != null)
        {
            if (placeholder.Pawn != null) placeholder.Pawn.Remove(); // frees the cell
            placeholder.gameObject.SetActive(false);
        }

        if (CreateMonster(data, new Vector2Int(def.x, def.y)) == null) return false;
        spawnedElementIds.Add(def.id);
        return true;
    }

    private void OnMonsterDied(Monster monster)
    {
        monster.Died -= OnMonsterDied;
        activeMonsters.Remove(monster);
        Destroy(monster.gameObject); 
    }
}