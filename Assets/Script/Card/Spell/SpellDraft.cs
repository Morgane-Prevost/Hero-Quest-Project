using System;
using System.Collections.Generic;

// Spell group distribution (rulebook p.8): the wizard picks a group first, then the elf picks one of the
// remaining groups, the two left go to the wizard. Pure C# so it can be tested without a scene.
public class SpellDraft
{
    public const int SpellsPerGroup = 3;
    private static readonly SpellElement[] AllElements = (SpellElement[])Enum.GetValues(typeof(SpellElement));

    private readonly Dictionary<SpellElement, List<SpellCardSO>> groups = new Dictionary<SpellElement, List<SpellCardSO>>();
    private readonly List<SpellElement> wizardGroups = new List<SpellElement>();
    private SpellElement? elfGroup;

    public SpellDraft(IEnumerable<SpellCardSO> allSpells)
    {
        foreach (SpellElement e in AllElements) groups[e] = new List<SpellCardSO>();

        foreach (SpellCardSO spell in allSpells)
        {
            if (spell == null) throw new ArgumentException("Empty slot in the spell list");
            if (groups[spell.element].Contains(spell)) throw new ArgumentException("Duplicate spell: " + spell.name);
            groups[spell.element].Add(spell);
        }
        foreach (SpellElement e in AllElements)
            if (groups[e].Count != SpellsPerGroup)
                throw new ArgumentException("Group " + e + " has " + groups[e].Count + " spells, expected " + SpellsPerGroup);
    }

    public bool IsWizardTurn => wizardGroups.Count == 0;
    public bool IsDone => elfGroup.HasValue;

    public IReadOnlyList<SpellCardSO> Group(SpellElement e) => groups[e];

    public bool IsAvailable(SpellElement e) => !wizardGroups.Contains(e) && elfGroup != e;

    // Returns false if the group is already taken or the choice is over.
    public bool Pick(SpellElement e)
    {
        if (IsDone || !IsAvailable(e)) return false;

        if (IsWizardTurn) { wizardGroups.Add(e); return true; }

        elfGroup = e;
        foreach (SpellElement rest in AllElements)          // the two groups left go to the wizard
            if (IsAvailable(rest)) wizardGroups.Add(rest);
        return true;
    }

    public List<SpellCardSO> ElfSpells => IsDone ? new List<SpellCardSO>(groups[elfGroup.Value]) : new List<SpellCardSO>();

    public List<SpellCardSO> WizardSpells
    {
        get
        {
            var list = new List<SpellCardSO>();
            if (IsDone) foreach (SpellElement e in wizardGroups) list.AddRange(groups[e]);
            return list;
        }
    }
}