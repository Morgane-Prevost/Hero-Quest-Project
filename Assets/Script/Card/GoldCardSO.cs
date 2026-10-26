using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Treasure/Gold")]
public class GoldCardSO : TreasureCardSO
{
    public int goldAmount;
    public override void Resolve(ITreasureContext context) => context.AddGold(goldAmount);
}
