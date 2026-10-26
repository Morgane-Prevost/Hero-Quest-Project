using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Treasure/Potion")]
public class PotionCardSO : TreasureCardSO
{
    public PotionSO potion;

    public override void Resolve(ITreasureContext context) => context.AddPotion(potion);
}
