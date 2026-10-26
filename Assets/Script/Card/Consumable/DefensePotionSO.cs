using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Treasure/Potion/Defense")]
public class DefensePotionSO : PotionCardSO
{
    public int bonusDefenseDice = 2;

    public override void Use(IPotionTarget target, IRandomPicker picker)
    {
       target.AddDefenseDiceNextDefense(bonusDefenseDice);
    }
}
