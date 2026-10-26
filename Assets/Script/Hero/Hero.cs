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

        // Needs to be modified to create and add the weapon and armor cards before assigning them to the hero. For now, we will just assign the references from the HeroData.
        weapon = heroData.weapon;
        armors = heroData.armor;

        gold = heroData.gold;
    }

    public void Move()
    {
        // Roll the movement dices and display where the hero can move on the board
        // Wait for the player to select a tile to move to
        // Move the hero to the selected tile
    }

    public void AttackAction()
    {
        // Check the weapon to see what enemy can be attacked

        // Look at every enemy that can be attacked

        // Wait for the player to select an enemy to attack

        // Roll the attack dices and calculate the damage

        // Apply the damage to the enemy
    }

    public void SpellAction()
    {
        // Look at every enemy in the field of view that can be attacked

        // Wait for the player to select an enemy to attack

        // Roll the attack dices and calculate the damage

        // Apply the damage to the enemy
    }

    public void LookForTreasureAction()
    {
        if (canLookForTreasure())
        {
            // Look for treasure in the room
            // If treasure is found draw a treasure card
            // If it draws a trap card apply it 
            // Else give the treasure to the hero
        }
        else
        {
            // Display a message that the hero cannot look for treasure
        }
    }

    public void LookForSecretDoorAction()
    {
        // Check if there is monster in the FOV
        // Check if there is a secret door in the room, reveal it else display a message that there is no secret door
    }

    public void LookForTrapAction()
    {
        // Check if there is a monster in the FOV
        // Check if there is a trap in the room, mark it else display a message that there is no trap
    }

    public void DisarmTrapAction()
    {
        //If dwarf
        //Move on the trap and roll a fighting dice (1/6)
        // If the hero rolls a 1, he activates the trap and takes damage, else he disarms the trap

        // Check if he has a tool bag in his inventory
        // Move on the trap and roll a fighting dice (1/2)
        // If the hero rolls a 1, he activates the trap and takes damage, else he disarms the trap
        // Delete the trap
    }

    private bool canLookForTreasure()
    {
        //Check the list if the room is in the list && if there is monster in the room
        //return false else return true
        return true;
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

    // Inventory management methods
    #endregion
}
