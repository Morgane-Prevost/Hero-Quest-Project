using UnityEngine;

[CreateAssetMenu(menuName = "GameDatas/Equipement/Artifact/FortuneLongsword")]
public class FortuneLongswordData : ArtifactWeaponData
{
    public override bool HasAbility => true;
    public override AttackMoment AbilityMoment => AttackMoment.AfterRoll; // reroll needs the roll first
    public override void ActivateAbility(AttackContext context) => context.rerollsAllowed += 1;
}
