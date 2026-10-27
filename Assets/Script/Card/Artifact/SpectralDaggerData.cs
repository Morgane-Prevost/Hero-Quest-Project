using UnityEngine;

[CreateAssetMenu(menuName = "GameDatas/Equipement/Artifact/SpectralDagger")]
public class SpectralDaggerData : ArtifactWeaponData
{
    public override bool HasAbility => true;
    public override AttackMoment AbilityMoment => AttackMoment.BeforeRoll;
    public override void ActivateAbility(AttackContext context)
    {
        context.targetCanDefend = false;
    }
}
