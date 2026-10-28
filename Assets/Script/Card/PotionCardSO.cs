using UnityEngine;

// Base for treasure potions. One subclass per effect.
// Drawing the card gives the potion to the hero, using it applies the effect.
// Stateless: the asset can sit in several inventories, so state lives on the target.
// No turn check: using a potion is not an action (rulebook p.12, p.16, p.21).
public abstract class PotionCardSO : TreasureCardSO
{

    // Potions are removed from the pile once drawn (ReturnsToPile stays false)
    public override void Resolve(ITreasureContext context) => context.AddPotion(this);

    // The picker is passed in so random parts (healing roll) can be replayed in test games
    public abstract void Use(IPotionTarget target, IRandomPicker picker);
}
