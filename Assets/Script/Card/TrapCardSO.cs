using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Treasure/Trap")]
public class TrapCardSO : TreasureCardSO
{
    public int lifeLoss = 1;
    public bool endsTurn = true;

    public override bool ReturnsToPile => true;

    public override void Resolve(ITreasureContext context)
    {
        context.LoseLife(lifeLoss);
        if(endsTurn) { context.EndHeroTurn(); }
    }
}
