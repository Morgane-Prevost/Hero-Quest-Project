using UnityEngine;

// Base for every card a hero can consume (treasure potions now, armory consumables later).
// No turn check: drinking is not an action and can be done at any time,
// even at 0 body points
// A consumable asset must stay stateless: it can sit in several heroes' inventories.

public abstract class ConsumableCardSO : CardSO
{
    // The shared picker is passed in so any random part (healing roll) can be replayed in test games
    public abstract void Use(Hero hero, IRandomPicker picker);
}
