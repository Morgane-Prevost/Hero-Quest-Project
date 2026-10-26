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
    public int gold { get; set; }

    private List<CardSO> inventory = new List<CardSO>();
    private bool hasAToolBag = false;

    public void Initialize(HeroData heroData, string name)
    {
        heroName = name;
        bodyPoints = heroData.bodyPoints;
        mindPoints = heroData.mindPoints;
        attackDices = heroData.attackDices;
        defenseDices = heroData.defenseDices;
        movementDices = heroData.movementDices;

        // Needs to be modified to create and add the weapon and armor cards before assigning them to the hero. For now, we will just assign the references from the HeroData.
        weapon = heroData.weapon;
        armors = heroData.armor;

        gold = heroData.gold;
    }

    #region Inventory Management
    // Inventory management methods

    public void equipWeapon(WeaponData newWeapon)
    {
        if (canEquipWeapon(newWeapon))
        {
            RemoveCardFromInventory(newWeapon);
            weapon = newWeapon;
            attackDices = newWeapon.attackDice;
        }
        else
        {
            Debug.LogWarning("Cannot equip weapon: " + newWeapon.displayName);
        }
    }
    public void unequipWeapon()
    {
        AddCardToInventory(weapon);
        weapon = null;
        attackDices = 0;
    }

    public void EquipArmor(ArmorData newArmor)
    {
        if (canEquipArmorPiece(newArmor))
        {
            RemoveCardFromInventory(newArmor);
            armors.Add(newArmor);
            defenseDices += newArmor.defenseDice;
        }
        else
        {
            Debug.LogWarning("Cannot equip armor: " + newArmor.displayName);
        }
    }
    public void unEquipArmor(ArmorData armor)
    {
        if (armors.Contains(armor))
        {
            AddCardToInventory(armor);
            armors.Remove(armor);
            defenseDices -= armor.defenseDice;
        }
    }
    
    public bool canEquipWeapon(WeaponData weaponToCheck)
    {
        if (weaponToCheck == null || weapon != null) return false;
        if (!weaponToCheck.useWithWizard && heroType == HeroType.Wizard) return false;
        if (!weaponToCheck.useWithShield && armors.Exists(armor => armor.armorType == ArmorType.Shield)) return false;
        return true;
    }
    public bool canEquipArmorPiece(ArmorData armorToCheck)
    {
        if (armorToCheck != null && armors.Contains(armorToCheck)) return false;
        if (!armorToCheck.useWithWizard && heroType == HeroType.Wizard) return false;
        if (!armorToCheck.useWithStaff && weapon.weaponType == WeaponType.Staff) return false;
        if (!armorToCheck.useWithAxe && weapon.weaponType == WeaponType.Axe) return false;

        foreach (ArmorData armor in armors)
        {
            if (!armorToCheck.useWithOtherArmor && !armor.useWithOtherArmor) return false;
        }
        return true;
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

    public void GiveCardToHero(CardSO card, Hero otherHero)
    {
        if (card != null && inventory.Contains(card))
        {
            RemoveCardFromInventory(card);
            otherHero.AddCardToInventory(card);
        }
    }

    public bool UseConsumable(ConsumableCardSO consumable, IRandomPicker picker)
    {
        if(consumable == null || !inventory.Contains(consumable)) return false;

        consumable.Use(this, picker);
        RemoveCardFromInventory(consumable);
        return true;
    }

    // Inventory management methods
    #endregion

    #region Pending consumable effects
    // State created by consumables. It lives on the hero, never on the card asset.
    // Counters (not bools) because the rulebook allows drinking several potions (p.16).

    public int PendingAttackDiceBonus { get; private set; }
    public int PendingDefenseDiceBonus { get; private set; }
    public int PendingExtraAttacks { get; private set; }

    public void AddAttackDiceBonus(int dice) => PendingAttackDiceBonus += dice;
    public void AddDefenseDiceBonus(int dice) => PendingDefenseDiceBonus += dice;
    public void AddExtraAttacks(int count) => PendingExtraAttacks += count;

    // Each Consume method returns the stored bonus and resets it to 0 (single use).
    public int ConsumeAttackDiceBonus()
    {
        int bonus = PendingAttackDiceBonus;
        PendingAttackDiceBonus = 0;
        return bonus;
    }

    public int ConsumeDefenseDiceBonus()
    {
        int bonus = PendingDefenseDiceBonus;
        PendingDefenseDiceBonus = 0;
        return bonus;
    }

    public int ConsumeExtraAttacks()
    {
        int extra = PendingExtraAttacks;
        PendingExtraAttacks = 0;
        return extra;
    }
    #endregion
}
