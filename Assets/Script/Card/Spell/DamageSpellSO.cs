using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Spells/Damage Spell")]
public class DamageSpellSO : SpellCardSO
{
    private const int ResistFrom = 5; // a 5 or a 6 resists (same on both cards)

    public int damage = 1;
    public int resistDice = 1;          // red dice rolled by the target

    public override void CastOnMonster(ISpellMonsterTarget target, IRandomPicker picker)
    {
        int resisted = 0;
        for(int i = 0; i< resistDice; i++)
        {
            if (picker.Pick(6) + 1 >= ResistFrom) resisted++;

        }

        int finalDamage = Mathf.Max(0, damage - resisted);
        if (finalDamage > 0) target.TakeDamage(finalDamage);
    }
}
