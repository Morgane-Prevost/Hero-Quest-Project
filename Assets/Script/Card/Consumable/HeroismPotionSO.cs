using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Consumable/Potion/Heroism")]
public class HeroismPotionSO : PotionSO
{
    public override void Use(IPotionTarget target, IRandomPicker picker)
    {
        target.AttackTwiceNextAttack();
    }
}
