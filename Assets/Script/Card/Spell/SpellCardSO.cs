using UnityEngine;

public enum SpellElement { Air, Fire, Water, Earth }

// What a spell is allowed to ask af a hero. Implemented by Hero (Heal already exists there)
public interface ISpellHeroTarget
{
    void Heal(int amount);
    void AddEffect(HeroEffect effect, int value);
}


// What a spell is allowed to ask of a monster. Implemented by Monster (TakeDamage already exists there).
public interface ISpellMonsterTarget
{
    void TakeDamage(int amount); 
    bool HasTag(string tag);
    void SkipNextTurn();
    void FallAsleep();
    bool TryBreakSleep(IRandomPicker picker); // true if the monster is free (or was not asleep)
}

// Base of the 12 hero spells. One subclass per KIND of effect, values live in the assets.
// Stateless: usage is tracked per hero by SpellBook.
// No turn check and no line of sight here: both belong to the caller(rulebook p.14)
public class SpellCardSO : CardSO
{
    public SpellElement element;

    // The picker is passed in so random parts can be replayed in test games
    public virtual void CastOnHero(ISpellHeroTarget target, IRandomPicker picker) => throw new System.InvalidOperationException($"{displayName} cannot target a hero");

    public virtual void CastOnMonster(ISpellMonsterTarget target, IRandomPicker picker) => throw new System.InvalidOperationException($"{displayName} cannot target a monster");

    // Spells with limits on their target (sleep) override it. The caller must not offer invalid targets.
    public virtual bool CanTarget(ISpellMonsterTarget target) => true;
}
