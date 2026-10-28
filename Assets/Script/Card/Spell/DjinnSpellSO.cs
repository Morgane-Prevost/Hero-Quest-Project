using UnityEngine;

// Djinn: ONE action among two, chosen by the caster: open a door, or attack a visible monster.
// Door: CastOnDoor. Attack: CastOnMonster. Using one spends the spell (SpellBook).
[CreateAssetMenu(menuName = "Cards/Spells/Djinn Spell")]
public class DjinnSpellSO : SpellCardSO
{
    // Combat dice faces(same mapping as ActionManager.RollWhiteDice) :
    // 1-3 skull, 4-5 white shield, 6 black shield. Duplicated until the "Dice" card shares it.
    private const int SkullUpTo = 3;
    private const int BlackShieldFace = 6;

    public int attackDice = 5;

    public override bool CastOnDoor(ISpellDoorTarget target, string doorId) => target.OpenDoor(doorId);

    public override void CastOnMonster(ISpellMonsterTarget target, IRandomPicker picker)
    {
        int skulls = 0;
        for (int i = 0; i < attackDice; i++)
            if (picker.Pick(6) + 1 <= SkullUpTo) skulls++;

        if (skulls == 0) return;   // the defender only rolls if at least one skull (rulebook p.12)

        int blocked = 0;           // the monster blocks with BLACK shields
        for (int i = 0; i < target.DefenseDice; i++)
            if (picker.Pick(6) + 1 == BlackShieldFace) blocked++;

        int damage = Mathf.Max(0, skulls - blocked);
        if (damage > 0) target.TakeDamage(damage);
    }
}
