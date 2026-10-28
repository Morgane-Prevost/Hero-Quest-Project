using System.Collections.Generic;
using UnityEngine;

public enum DreadArea { 
    OneHero, 
    CasterRoom, 
    Line 
}

// Fireball, Fire storm and Lightning: same class, only the values differ.
[CreateAssetMenu(menuName = "Cards/Dread Spells/Damage Spell")]
public class DreadDamageSpellSO : DreadSpellSO
{
    private const int ResistFrom = 5;   // same rule as DamageSpellSO, duplicated until the "Dice" card

    public DreadArea area = DreadArea.OneHero;
    public int damage = 2;
    public int resistDice = 2;          // red dice rolled by EACH victim (0 = no roll: lightning)

    public int RollFinalDamage(IRandomPicker picker)
    {
        int resisted = 0;
        for (int i = 0; i < resistDice; i++)
            if (picker.Pick(6) + 1 >= ResistFrom) resisted++;
        return Mathf.Max(0, damage - resisted);
    }

    private void Hit(IDamageable victim, IRandomPicker picker)
    {
        int finalDamage = RollFinalDamage(picker);
        if (finalDamage > 0) victim.TakeDamage(finalDamage);
    }

    public override void CastOnHero(IDreadHeroTarget target, IRandomPicker picker) => Hit(target, picker);

    public override bool IsValidCast(DreadCast cast)
    {
        switch (area)
        {
            case DreadArea.OneHero: return cast.hero != null;
            case DreadArea.CasterRoom: return cast.world != null && cast.caster != null && cast.world.IsInRoom(cast.caster.cell);   // never in a corridor
            case DreadArea.Line: return cast.world != null && cast.caster != null && IsDirection(cast.direction);
            default: return false;
        }
    }

    private static bool IsDirection(Vector2Int d) => d != Vector2Int.zero && Mathf.Abs(d.x) <= 1 && Mathf.Abs(d.y) <= 1;

    public override void Cast(DreadCast cast, IRandomPicker picker)
    {
        switch (area)
        {
            case DreadArea.OneHero: CastOnHero(cast.hero, picker); break;
            case DreadArea.CasterRoom: HitCells(cast, cast.world.AreaOf(cast.caster.cell), picker); break;
            case DreadArea.Line: HitCells(cast, cast.world.LineFrom(cast.caster.cell, cast.direction), picker); break;
        }
    }

    // Every hero and monster on the cells, except the caster.
    private void HitCells(DreadCast cast, IEnumerable<Vector2Int> source, IRandomPicker picker)
    {
        var cells = new List<Vector2Int>(source);   // copy: monsters may die while we loop
        foreach (Vector2Int c in cells)
        {
            IDamageable victim = cast.world.VictimAt(c);
            if (victim == null || object.ReferenceEquals(victim, cast.caster)) continue;
            Hit(victim, picker);
        }
    }
}