using UnityEngine;


// Water healing and Earth healing: same class, two assets
[CreateAssetMenu(menuName = "Cards/Spells/Heal Spell")]
public class HealSpellSO : SpellCardSO
{
    public int amount = 4;
    public override void CastOnHero(ISpellHeroTarget target, IRandomPicker picker)
    {
        target.Heal(amount);
    }

    public override SpellTargets Targets => SpellTargets.Hero;

}
