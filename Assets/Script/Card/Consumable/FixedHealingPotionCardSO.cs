using UnityEngine;

// Quest potion: restores a FIXED number of body points ("up to N lost points": Heal never goes above the maximum).
// Quests 2, 3: 4 points. Quest 5: 2 points. (HealingPotionCardSO keeps the random red-dice version.)
[CreateAssetMenu(menuName = "Cards/Treasure/Potion/Fixed Healing")]
public class FixedHealingPotionCardSO : PotionCardSO
{
    public int healAmount = 4;

    public override void Use(IPotionTarget target, IRandomPicker picker) => target.Heal(healAmount);
}
