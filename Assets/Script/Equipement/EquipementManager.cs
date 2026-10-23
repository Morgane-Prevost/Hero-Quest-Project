using System.Collections.Generic;
using UnityEngine;

public class EquipementManager : MonoBehaviour
{
    [SerializeField] private List<WeaponData> weapons;
    void createEquipement()
    {
        foreach (WeaponData weaponData in weapons)
        {
            GameObject weaponObject = Instantiate(weaponData.equipement.cardPrefab, Vector3.zero, Quaternion.identity);
            weaponObject.SetActive(false);
        }
    }
}
