using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "GameDatas/Equipement/WeaponData")]
public class WeaponData : CardSO
{
    public Equipement equipement;
    public int attackDice;

    public bool canDiagoAttack;
    public bool canCloseRangeAttack;
    public bool canBeThrown;
    public bool canVisionAttack;

    public bool useWithWizard;
    public bool useWithShield;
}
