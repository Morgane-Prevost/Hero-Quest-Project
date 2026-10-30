using UnityEngine;

public enum HeroEffect
{
    SwiftWind,
    MistVeil,
    ThroughStone,
    Courage,
    RockSkin,
    //States put on a hero by terror spells
    SkipTurn,   // tornado
    Asleep,     // sleep: no move, no attack, no defense
    Horror,     // only on attack die
    Controlled  // Zargon moves him and attacks with him
}


[CreateAssetMenu(menuName = "Cards/Spells/Hero Effect Spell")]
public class HeroEffectSpellSO : SpellCardSO
{
    public HeroEffect effect;
    public int value;

    public override void CastOnHero(ISpellHeroTarget target, IRandomPicker picker) => target.AddEffect(effect, value);

    public override SpellTargets Targets => SpellTargets.Hero;

    public override bool EffectImplemented => effect == HeroEffect.SwiftWind;
}
