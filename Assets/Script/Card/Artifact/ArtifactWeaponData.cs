using UnityEngine;

// Weapon artifact : stats com from WeaponData, so Hero.equipWeapon works unchanged.
[CreateAssetMenu(fileName = "ArtifactWeaponData", menuName = "GameDatas/Equipement/ArtifactWeaponData")]
public class ArtifactWeaponData : WeaponData, IArtifact, IAttackModifier
{
    [SerializeField] private ArtifactInfo info;
    public ArtifactInfo Info => info;

    [Header("Bonus against one monster family (printed on the card")]
    public string bonusAgainstTag;      //"orc", "undead"... empty = no bonus
    public int bonusAttackDice;         // Mystic sword: +1 against undead
    public int bonusAttacks;            // Orc slayer: +1 attack against orcs

    public void ModifyAttack(AttackContext context)
    {
        if (string.IsNullOrEmpty(bonusAgainstTag)) return;
        if (System.Array.IndexOf(context.targetTags, bonusAgainstTag) < 0) return;

        context.attackDice += bonusAttackDice;
        context.attackDice += bonusAttacks;
    }

    public virtual bool HasAbility => false;
    public virtual AttackMoment AbilityMoment => AttackMoment.BeforeRoll;
    public virtual void ActivateAbility(AttackContext context) { }
}
