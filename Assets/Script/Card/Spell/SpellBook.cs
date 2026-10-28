using System;
using System.Collections.Generic;

// Spells of ONE hero. Each spell can be cast once per quest, then comes back 
// Pure C# so it can be tested without a scene. 
public class SpellBook
{
    private readonly List<SpellCardSO> spells = new List<SpellCardSO>();
    private readonly HashSet<SpellCardSO> used= new HashSet<SpellCardSO>();

    public SpellBook(IEnumerable<SpellCardSO> cards)
    {
        foreach (SpellCardSO spell in cards)
        {
            if (spell == null) throw new ArgumentException("Empty slot in the spell list");
            if (spells.Contains(spell)) throw new ArgumentException($"Duplicate spell: {spell.name}");
            spells.Add(spell);
        }
    }

    public bool IsAvailable(SpellCardSO spell) => spells.Contains(spell) && !used.Contains(spell);

    // Marks the spell as cast. Returns false if it was not available.
    public bool TryUse(SpellCardSO spell)
    {
        if (!IsAvailable(spell)) return false;
        used.Add(spell);
        return true;
    }

    public List<SpellCardSO> GetAvailable() => spells.FindAll(IsAvailable);

    // All spells come back between quests
    public void Restore() => used.Clear();
}
