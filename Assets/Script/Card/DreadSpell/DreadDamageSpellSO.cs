using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Dread Spells/Dread Damage Spell")]
public class DreadDamageSpellSO : DreadSpellSO
{
    private const int ResistFrom = 5;

    public int damage = 2;
    public int resistDice = 2;

    public int RollFinalDamage(IRandomPicker picker)
    {
        int resisted = 0;
        for (int i = 0; i < resistDice; i++)
        {
            if (picker.Pick(6) + 1 >= ResistFrom) resisted++;
        }
        return Mathf.Max(0, damage - resisted);
    }

    public override void CastOnHero(IDreadHeroTarget target, IRandomPicker picker)
    {
        int finalDamage = RollFinalDamage(picker);
        if(finalDamage>0) target.TakeDamage(finalDamage);
    }

}
