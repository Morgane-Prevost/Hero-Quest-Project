using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DreadSpellManager : MonoBehaviour
{
    public static DreadSpellManager Instance { get; private set; }

    private static readonly HeroEffect[] ReleasableEffects = { HeroEffect.Asleep, HeroEffect.Controlled, HeroEffect.Horror };


    //monster -> spell -> casts left
    private readonly Dictionary<Monster, Dictionary<DreadSpellSO, int>> castsLeft = new Dictionary<Monster, Dictionary<DreadSpellSO, int>>();
    private IRandomPicker picker;

    public void Initialize(IRandomPicker picker) => this.picker = picker;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (picker == null) picker = new SystemRandomPicker();
    }


    // Quest setup, from the quest notes. uses = 1 unless the notes say otherwise.
    public void Assign(Monster monster, DreadSpellSO spell, int uses = 1)
    {
        if (monster == null || spell == null || uses < 1) throw new ArgumentException("Assign: missing monster, spell or uses < 1");
        if (!castsLeft.TryGetValue(monster, out var spells)) castsLeft[monster] = spells = new Dictionary<DreadSpellSO, int>();
        if (spells.ContainsKey(spell)) throw new ArgumentException($"Duplicate spell: {spell.name}");
        spells[spell] = uses;
    }

    public void StartQuest() => castsLeft.Clear();

    public bool CanCast(Monster caster, DreadSpellSO spell)
        => caster != null && spell != null
        && castsLeft.TryGetValue(caster, out var spells)
        && spells.TryGetValue(spell, out int left) && left > 0;

    // The caller checks: it is Zargon's turn, the monster has not acted and can see the hero.
    // Returns false if the spell was not available (nothing happens, nothing is spent).
    public bool CastOnHero(Monster caster, DreadSpellSO spell, IDreadHeroTarget target)
    {
        if (caster == null || spell == null || target == null)
        {
            Debug.LogError("CastOnHero: missing caster, spell or target");
            return false;
        }
        if (!CanCast(caster, spell)) return false;

        spell.CastOnHero(target, picker);   // may throw: then the spell is not spent
        castsLeft[caster][spell]--;
        return true;
    }

    // One entry point for every Terror spell. The caller checks: it is Zargon's turn, the caster has not acted,
    // and the hero / area is in the caster's sight. Returns false if the spell or the target is not valid:
    // nothing happens, nothing is spent.
    public bool Cast(Monster caster, DreadSpellSO spell, DreadCast cast)
    {
        if (caster == null || spell == null || cast == null)
        {
            Debug.LogError("Cast: missing caster, spell or cast");
            return false;
        }
        if (!CanCast(caster, spell)) return false;

        cast.caster = caster;
        if (!spell.IsValidCast(cast)) return false;

        spell.Cast(cast, picker);   // may throw: then the spell is not spent
        castsLeft[caster][spell]--;
        return true;
    }

    // Called by TurnManager at the start of the hero's turn: one release roll per active effect.
    public void RollReleases(Hero hero)
    {
        if (hero == null) return;
        foreach (HeroEffect effect in ReleasableEffects)
            if (hero.HasEffect(effect)) hero.TryBreakEffect(effect, picker);
    }
}
