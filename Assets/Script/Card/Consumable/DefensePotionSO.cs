using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Consumable/Potion/Defense")]
public class DefensePotionSO : PotionSO
{
    public int bonusDefenseDice = 2;

    public override void Use(IPotionTarget target, IRandomPicker picker)
    {
       target.AddDefenseDiceNextDefense(bonusDefenseDice);
    }
}
