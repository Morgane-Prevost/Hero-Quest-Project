using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Consumable/Potion/Strength")]
public class StrengthPotionSO : PotionSO
{
    public int bonusAttackDice = 2; // +2 combat dice on the next attack (physical card)

    public override void Use(IPotionTarget target, IRandomPicker picker)
    {
       target.AddAttackDiceNextAttack(bonusAttackDice);
    }
}
