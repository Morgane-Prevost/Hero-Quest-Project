using UnityEngine;

public enum SpellElement { Air, Fire, Water, Earth }

// What a spell is allowed to ask af a hero. Implemented by Hero (Heal already exists there)
public interface ISpellHeroTarget
{
    void Heal(int amount);
}

// Base of the 12 hero spells. One subclass per KIND of effect, values live in the assets.
// Stateless: usage is tracked per hero by SpellBook.
// No turn check and no line of sight here: both belong to the caller(rulebook p.14)
public class SpellCardSO : CardSO
{
    public SpellElement spellElement;

    // The picker is passed in so random parts can be replayed in test games
    public virtual void CastOnHero(ISpellHeroTarget target, IRandomPicker picker) => throw new System.InvalidOperationException($"{displayName} cannot target a hero");
}
