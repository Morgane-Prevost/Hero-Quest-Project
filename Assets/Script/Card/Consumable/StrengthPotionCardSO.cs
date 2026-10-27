using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Treasure/Potion/Strength")]
public class StrengthPotionCardSO : PotionCardSO
{
    public int extraAttackDice = 2; // +2 combat dice on the next attack (physical card)

    public override void Use(IPotionTarget target, IRandomPicker picker)
    {
       target.AddAttackDiceNextAttack(extraAttackDice);
    }
}
