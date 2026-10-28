using UnityEngine;
using System.Collections.Generic;
using HeroQuest.Board;
using UnityEngine.UI;

public class HeroManager : MonoBehaviour
{
    [SerializeField] private BoardBuilder board;
    [SerializeField] private List<HeroData> heroes;


    public static HeroManager Instance { get; private set; }

    // Every hero AND ally created for the game (used by the quest system).
    private readonly List<Hero> allHeroes = new List<Hero>();
    public IReadOnlyList<Hero> Heroes => allHeroes;

    public bool HasHeroes => allHeroes.Exists(h => h != null && !h.IsAlly);

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

    public Hero GetById(string id) => allHeroes.Find(h => h != null && h.Id == id);

    public Hero GetByType(HeroType type) => allHeroes.Find(h => h != null && !h.IsAlly && h.Type == type);

    // One start cell per hero (the first cells of the list, in order).
    public void CreateCharacter(List<Vector2Int> startCells)
    {
        int index = 0;
        foreach (HeroData heroData in heroes)
        {
            Vector2Int startCell = startCells.Count > 0
                ? startCells[Mathf.Min(index, startCells.Count - 1)]
                : new Vector2Int(0, 0);
            Spawn(heroData, heroData.name, startCell); // To be modified to take the name from the character creation UI
            index++;
        }
    }

    // Heroes already exist (second quest): put them back on the start cells, fresh for the new quest.
    public void PlaceHeroes(IReadOnlyList<Vector2Int> cells)
    {
        int index = 0;
        foreach (Hero hero in allHeroes)
        {
            if (hero == null || hero.IsAlly) continue;
            hero.ResetForNewQuest();
            hero.Pawn.Remove();                                   // frees the previous cell
            hero.Pawn.PlaceAt(cells[Mathf.Min(index, cells.Count - 1)]);
            index++;
        }
    }

    // Escorted NPC (Sir Ragnar): a Hero flagged IsAlly, so monsters can target it like a hero.
    public Hero CreateAlly(HeroData template, string id, string displayName, Vector2Int cell)
    {
        Hero ally = Spawn(template, displayName, cell);
        if (ally == null) return null;
        ally.IsAlly = true;
        ally.Id = id;
        return ally;
    }

    public void ClearAllies()
    {
        for (int i = allHeroes.Count - 1; i >= 0; i--)
        {
            Hero h = allHeroes[i];
            if (h != null && !h.IsAlly) continue;
            allHeroes.RemoveAt(i);
            if (h != null) Destroy(h.gameObject);
        }
    }

    private Hero Spawn(HeroData heroData, string heroName, Vector2Int cell)
    {
        GameObject heroObject = Instantiate(heroData.prefab, Vector3.zero, Quaternion.identity);
        GridPawn gridPawnComponent = heroObject.AddComponent<GridPawn>();
        gridPawnComponent.board = board;
        Hero heroComponent = heroObject.AddComponent<Hero>();
        heroComponent.Initialize(heroData, heroName, cell);
        allHeroes.Add(heroComponent);
        return heroComponent;
    }
}
