using UnityEngine;

public class Hero : MonoBehaviour
{
    public string heroName;

    public int bodyPoints;
    public int mindPoints;
    public int attackDices;
    public int defenseDices;

    public int movementDices;

    //To add when classes are implemented

    // public Equipment weapon;
    // public Equipment armor;

    public void Initialize(HeroData heroData, string name)
    {
        heroName = name;
        bodyPoints = heroData.bodyPoints;
        mindPoints = heroData.mindPoints;
        attackDices = heroData.attackDices;
        defenseDices = heroData.defenseDices;
        movementDices = heroData.movementDices;
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


}
