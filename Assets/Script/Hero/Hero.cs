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

public class Hero : MonoBehaviour, IPotionTarget
{
    public string heroName { get; set; }
    HeroType heroType { get; set; }
    public int bodyPoints { get; set; }
    public int mindPoints { get; set; }

    public int currentBodyPoints { get; private set; }
    public int attackDices { get; set; }
    public int defenseDices { get; set; }

    public int movementDices { get; set; }

    public WeaponData weapon;
    public List<ArmorData> armors;
    public int gold { get; set; }

    private List<CardSO> inventory = new List<CardSO>();
    private bool hasAToolBag = false;

    private int pendingAttackDiceBonus;
    private int pendingDefenseDiceBonus;
    private int pendingExtraAttacks;

    public void Initialize(HeroData heroData, string name)
    {
        heroName = name;
        bodyPoints = heroData.bodyPoints;
        currentBodyPoints = bodyPoints;
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

    public bool UsePotion(PotionCardSO potion, IRandomPicker picker)
    {
        if (potion == null || !inventory.Contains(potion)) return false;

        potion.Use(this, picker);
        RemoveCardFromInventory(potion);
        return true;
    }

    // Inventory management methods
    #endregion

    #region IPotionTarget
    public void Heal(int amount)
    {
        // bodyPoints holds the starting total
        currentBodyPoints = Mathf.Min(currentBodyPoints + amount, bodyPoints);
    }
    public void AddAttackDiceNextAttack(int extraDice) => pendingAttackDiceBonus += extraDice;
    public void AddDefenseDiceNextDefense(int extraDice) => pendingDefenseDiceBonus += extraDice;
    public void AttackTwiceNextAttack() => pendingExtraAttacks++;

    // Called by the combat code when the roll really happens: returns the bonus and resets it (single use).
    public int ConsumeAttackDiceBonus() => TakeAndReset(ref pendingAttackDiceBonus);
    public int ConsumeDefenseDiceBonus() => TakeAndReset(ref pendingDefenseDiceBonus);
    public int ConsumeExtraAttacks() => TakeAndReset(ref pendingExtraAttacks);

    private static int TakeAndReset(ref int value)
    {
        int taken = value;
        value = 0;
        return taken;
    }

    #endregion
}
