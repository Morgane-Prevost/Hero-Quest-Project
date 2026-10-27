using UnityEngine;

public enum AttackMoment
{
    BeforeRoll = 0,
    AfterRoll = 1
}

// Facts about the attack being declared. Filled by the combat code, edited by artifacts.
public class AttackContext
{
    public string[] targetTags = new string[0];     // copied from the target mosnter
    public int attackDice;                          // combat code starts it at weapon dice (+ potion bonus)
    public int attacksAllowed = 1;                   // the player may use fewer
    public bool targetCanDefend = true; 
    public int rerollsAllowed;                      // attack dice the player may reroll after the roll
}

public interface IAttackModifier
{
    //Automatic effects printed on the card (no player choice involved)
    void ModifyAttack(AttackContext context);
}
