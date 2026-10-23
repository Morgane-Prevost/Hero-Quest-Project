using System.Collections.Generic;
using UnityEngine;

public class EquipementManager : MonoBehaviour
{
    [SerializeField] private List<WeaponData> weapons;
    [SerializeField] private List<ArmorData> armors;
    [SerializeField] private List<ConsumableData> consumables;
    [SerializeField] private List<PassiveData> passives;

    void createEquipement()
    {
        foreach (WeaponData weaponData in weapons)
        {
            GameObject weaponObject = Instantiate(weaponData.equipement.cardPrefab, Vector3.zero, Quaternion.identity);
            weaponObject.SetActive(false);
        }

        foreach (ArmorData armorData in armors)
        {
            GameObject armorObject = Instantiate(armorData.equipement.cardPrefab, Vector3.zero, Quaternion.identity);
            armorObject.SetActive(false);
        }

        foreach (ConsumableData consumableData in consumables)
        {
            GameObject consumableObject = Instantiate(consumableData.equipement.cardPrefab, Vector3.zero, Quaternion.identity);
            consumableObject.SetActive(false);
        }

        foreach (PassiveData passiveData in passives)
        {
            GameObject passiveObject = Instantiate(passiveData.equipement.cardPrefab, Vector3.zero, Quaternion.identity);
            passiveObject.SetActive(false);
        }
    }
}
