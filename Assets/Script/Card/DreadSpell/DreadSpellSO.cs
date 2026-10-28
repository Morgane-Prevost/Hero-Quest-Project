using UnityEngine;

public interface IDreadHeroTarget
{
    void TakeDamage(int amount);
    void AddEffect(HeroEffect effect, int value);
    bool TryBreakEffect(HeroEffect effect, IRandomPicker picker); // true if the hero is free (or was not affected)
}


// Base of Zargon's spell cards. One subclass per KIND of effect, values live in the assets.
// Stateless: casts left are tracked per monster by DreadSpellManager
// No turn check and no line of sight here: the caller does it (rulebook p.21)
public class DreadSpellSO : CardSO
{
    public virtual void CastOnHero(IDreadHeroTarget target, IRandomPicker picker)
       => throw new System.InvalidOperationException($"{displayName} cannot target a hero");
}
