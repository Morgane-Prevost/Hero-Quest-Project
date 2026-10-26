using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Treasure/Potion/Heroism")]
public class HeroismPotionSO : PotionCardSO
{
    public override void Use(IPotionTarget target, IRandomPicker picker)
    {
        target.AttackTwiceNextAttack();
    }
}
