using UnityEngine;

[CreateAssetMenu(fileName = "ArmorData", menuName = "GameDatas/Equipement/ArmorData")]
public class ArmorData : ScriptableObject
{
    public Equipement equipement;
    public int defenseDice;
    public int movementPenalty;

    public bool useWithWizard;
    public bool useWithOtherArmor;
    public bool useWithAxe;
    public bool useWithStaff;
}
