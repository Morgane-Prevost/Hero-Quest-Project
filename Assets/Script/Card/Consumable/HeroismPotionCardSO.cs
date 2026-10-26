using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Treasure/Potion/Heroism")]
public class HeroismPotionCardSO : PotionCardSO
{
    public override void Use(IPotionTarget target, IRandomPicker picker)
    {
        target.AttackTwiceNextAttack();
    }
}
