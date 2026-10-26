using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EquipementManager : MonoBehaviour
{
    private Effect effects;

    [SerializeField] private List<WeaponData> weapons;
    [SerializeField] private List<ArmorData> armors;
    [SerializeField] private List<ConsumableData> consumables;
    [SerializeField] private List<PassiveData> passives;

    void Awake()
    {
        effects = this.AddComponent<Effect>();
    }

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

    void useConsumableEffect(ConsumableData consumableData, Hero hero)
    {
        if (consumableData.isSpeedPotion)
        {
            effects.speedPotionEffect(hero);
        }
        else if (consumableData.isBlessedWater)
        {
            effects.blessedWaterEffect();
        }
    }
}
