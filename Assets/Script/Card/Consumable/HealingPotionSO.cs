using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Consumable/Potion/Healing")]
public class HealingPotionSO : PotionSO
{
    public int redDice = 1;

    public override void Use(IPotionTarget target, IRandomPicker picker)
    {
        int total = 0;
        for (int i = 0; i < redDice; i++) total += picker.Pick(6) + 1;
        target.Heal(total);
    }
}
