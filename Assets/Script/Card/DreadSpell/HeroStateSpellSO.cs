using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Dread Spells/Hero State Spell")]
public class HeroStateSpellSO : DreadSpellSO
{
    public HeroEffect state;
    public int value = 1;               // Horror: maximum attack dice
    public bool rollReleaseNow;         // true for the cards that say "immediately or during a next turn"

    public override void CastOnHero(IDreadHeroTarget target, IRandomPicker picker)
    {
        target.AddEffect(state, value);
        if (rollReleaseNow) target.TryBreakEffect(state, picker);
    }
}
