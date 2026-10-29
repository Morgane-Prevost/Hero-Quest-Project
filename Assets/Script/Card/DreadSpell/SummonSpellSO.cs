using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SummonGroup
{
    public MonsterData monster;
    public int count;
}

[Serializable]
public class SummonOutcome
{
    public int rollFrom = 1;   // red die faces, as written on the card
    public int rollTo = 6;
    public SummonGroup[] groups;
}

// Orc summoning and undead summoning: roll 1 red die, the card table gives the monsters.
[CreateAssetMenu(menuName = "Cards/Dread Spells/Summon Spell")]
public class SummonSpellSO : DreadSpellSO
{
    public SummonOutcome[] outcomes;

    public override bool IsValidCast(DreadCast cast) => cast.world != null && cast.caster != null;

    public override void Cast(DreadCast cast, IRandomPicker picker)
    {
        int roll = picker.Pick(6) + 1;
        SummonOutcome outcome = Find(roll);
        if (outcome == null) throw new InvalidOperationException($"{displayName}: no outcome for roll {roll}");

        // Nearest free cells of the caster's room/corridor. If there are not enough, the rest is not summoned.
        var free = new List<Vector2Int>(cast.world.FreeCellsAround(cast.caster.cell));
        int next = 0;
        foreach (SummonGroup group in outcome.groups)
        {
            for (int i = 0; i < group.count; i++)
            {
                bool placed = false;
                while (next < free.Count && !placed)
                    placed = cast.world.Spawn(group.monster, free[next++]) != null;
                if (!placed) Debug.LogWarning($"{displayName}: no free cell left for {group.monster.monsterName}");
            }
        }
    }

    private SummonOutcome Find(int roll)
    {
        foreach (SummonOutcome o in outcomes)
            if (roll >= o.rollFrom && roll <= o.rollTo) return o;
        return null;
    }
}