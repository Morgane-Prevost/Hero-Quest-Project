using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Dread Spells/Hero State Spell")]
public class HeroStateSpellSO : DreadSpellSO
{
    public HeroEffect state;
    public int value = 1;               // Horror: maximum attack dice
    public bool rollReleaseNow;         // true for the cards that say "immediately or during a next turn"
    public bool wholeArea;

    public override void CastOnHero(IDreadHeroTarget target, IRandomPicker picker)
    {
        target.AddEffect(state, value);
        if (rollReleaseNow) target.TryBreakEffect(state, picker);
    }

    public override bool IsValidCast(DreadCast cast) => wholeArea ? cast.world != null : cast.hero != null;

    public override void Cast(DreadCast cast, IRandomPicker picker)
    {
        if (!wholeArea) { CastOnHero(cast.hero, picker); return; }

        foreach (Vector2Int c in new System.Collections.Generic.List<Vector2Int>(cast.world.AreaOf(cast.cell)))
        {
            IDreadHeroTarget hero = cast.world.HeroAt(c);
            if (hero != null) CastOnHero(hero, picker);
        }
    }
}
