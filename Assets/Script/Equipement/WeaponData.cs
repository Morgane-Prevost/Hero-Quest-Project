using UnityEngine;

[CreateAssetMenu(fileName = "Equipement", menuName = "GameDatas/Equipement/WeaponData")]
public class WeaponData : ScriptableObject
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
