using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

public enum HeroType
{
    Barbarian = 0,
    Elf = 1,
    Dwarf = 2,
    Wizard = 3
}

public class Hero : MonoBehaviour
{
    public string heroName { get; set; }
    HeroType heroType { get; set; }
    public int bodyPoints { get; set; }
    public int mindPoints { get; set; }
    public int attackDices { get; set; }
    public int defenseDices { get; set; }

    public int movementDices { get; set; }

    public WeaponData weapon;
    public List<ArmorData> armors;
    public int gold;

    private List<CardSO> inventory;
    private bool hasAToolBag = false;

    public void Initialize(HeroData heroData, string name)
    {
        heroName = name;
        bodyPoints = heroData.bodyPoints;
        mindPoints = heroData.mindPoints;
        attackDices = heroData.attackDices;
        defenseDices = heroData.defenseDices;
        movementDices = heroData.movementDices;

        weapon = heroData.weapon;
        armors = heroData.armor;
        gold = heroData.gold;
    }

    public void SwapWeapon(WeaponData newWeapon)
    {
        AddCardToInventory(weapon);
        weapon = newWeapon;
        RemoveCardFromInventory(newWeapon);
        attackDices = newWeapon.attackDice;
    }

    public void EquipArmor(ArmorData newArmor)
    {
    }

    public void AddCardToInventory(CardSO card)
    {
        if (card != null) inventory.Add(card);
        if (card is PassiveData passiveData)
        {
            if (passiveData.passiveObjectName == PassiveObjectName.Toolbag) hasAToolBag = true;
        }
    }

    public void RemoveCardFromInventory(CardSO card)
    {
        if (card != null && inventory.Contains(card)) inventory.Remove(card);
    }
}
