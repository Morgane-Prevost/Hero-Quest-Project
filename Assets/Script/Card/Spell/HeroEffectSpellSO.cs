using UnityEngine;

public enum HeroEffect
{
    SwiftWind,
    MistVeil,
    ThroughStone,
    Courage, 
    RockSkin
}


[CreateAssetMenu(menuName = "Cards/Spells/Hero Effect Spell")]
public class HeroEffectSpellSO : SpellCardSO
{
    public HeroEffect effect;
    public int value;

    public override void CastOnHero(ISpellHeroTarget target, IRandomPicker picker) => target.AddEffect(effect, value);
}
