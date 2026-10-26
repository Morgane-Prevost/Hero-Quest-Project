using UnityEngine;


public abstract class TreasureCardSO : CardSO
{
    // Rule: only trap and wandering monster cards go back into the pile.
    // Gold and potion cards are removed until the next quest.
    public virtual bool ReturnsToPile => false;

    public abstract void Resolve(ITreasureContext context);
}
