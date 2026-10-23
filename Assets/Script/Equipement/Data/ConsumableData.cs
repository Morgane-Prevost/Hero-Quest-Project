using UnityEngine;

[CreateAssetMenu(fileName = "ConsumableData", menuName = "GameDatas/Equipement/ConsumableData")]
public class ConsumableData : ScriptableObject
{
    public Equipement equipement;

    public bool isSpeedPotion;
    public bool isBlessedWater;
}
