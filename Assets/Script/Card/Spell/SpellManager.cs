using System.Collections.Generic;
using UnityEngine;

// Singleton beacause Hero.SpellAction() will reach it.
public class SpellManager : MonoBehaviour
{

    public static SpellManager Instance {  get; private set; }

    private readonly Dictionary<Hero, SpellBook> books = new Dictionary<Hero, SpellBook>();
    private IRandomPicker picker;

    public void Initialize(IRandomPicker picker) => this.picker = picker;

    private void Awake()
    {
        if (Instance !=null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if(picker == null) picker = new SystemRandomPicker();
    }

    public void AssignSpells(Hero hero, IEnumerable<SpellCardSO> spells) => books[hero] = new SpellBook(spells);

    // Call it at the same place as TreasureManager.StartQuest: all spells come back
    public void StartQuest()
    {
        foreach(SpellBook book in books.Values) book.Restore();
    }

    public List<SpellCardSO> GetAvailableSpells(Hero hero) => books.TryGetValue(hero, out SpellBook book) ? book.GetAvailable() : new List<SpellCardSO>();

    public bool CanCast(Hero caster, SpellCardSO spell) => caster != null && books.TryGetValue(caster, out SpellBook book) && book.IsAvailable(spell);

    // The caller checks: it is the caster's turn, he has not acted yet, the target is visible.
    // Returns false if the spell was not available (nothing happens, nothing is spent). 
    public bool CastOnHero(Hero caster, SpellCardSO spell, ISpellHeroTarget target)
    {
        if (caster == null || spell == null || target == null)
        {
            Debug.LogError("CastOnHero: missing caster, spell or target");
            return false;
        }
        if (!books.TryGetValue(caster, out SpellBook book) || !book.IsAvailable(spell)) return false;

        spell.CastOnHero(target, picker);
        book.TryUse(spell);
        return true;
    }

    public bool CastOnMonster(Hero caster, SpellCardSO spell, ISpellMonsterTarget target)
    {
        if (caster == null || spell == null || target == null)
        {
            Debug.LogError("CastOnMonster: missing caster, spell or target");
            return false;
        }
        if (!books.TryGetValue(caster, out SpellBook book) || !book.IsAvailable(spell)) return false;
        if (!spell.CanTarget(target)) return false;   // e.g. sleep on the undead: nothing happens, nothing is spent

        spell.CastOnMonster(target, picker);   // may throw: then the spell is not spent
        book.TryUse(spell);
        return true;
    }

    // Same rules as the others: the caller checks turn, action and which doors are allowed.
    public bool CastOnDoor(Hero caster, SpellCardSO spell, ISpellDoorTarget level, string doorId)
    {
        if (caster == null || spell == null || level == null || string.IsNullOrEmpty(doorId))
        {
            Debug.LogError("CastOnDoor: missing caster, spell, level or door id");
            return false;
        }
        if (!books.TryGetValue(caster, out SpellBook book) || !book.IsAvailable(spell)) return false;
        if (!spell.CastOnDoor(level, doorId)) return false;   // unknown or already open door: nothing spent

        book.TryUse(spell);
        return true;
    }

}
