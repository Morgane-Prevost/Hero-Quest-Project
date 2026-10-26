using UnityEngine;

public enum WhiteDiceFaces
{
    Skull = 0,
    WhiteShield = 1,
    BlackShield = 2
}


public class ActionManager : MonoBehaviour
{
    //Takes a hero and a monster to do the fight and a bool is here to see who is attacking and who is defending
    //return damage inflicted
    public int Fight(Hero hero, Monster monster, bool heroIsAttacking)
    {
        if (heroIsAttacking)
        {

        }
        return 0;
    }

    public WhiteDiceFaces RollWhiteDice()
    {
        int result = Random.Range(1, 7);

        if (result <= 3)
        {
            return WhiteDiceFaces.Skull;
        }
        else if (result == 4 || result == 5)
        {
            return WhiteDiceFaces.WhiteShield;
        }
        else
        {
            return WhiteDiceFaces.BlackShield;
        }
    }

    public int RollRedDice()
    {
        return Random.Range(1, 7);
    }
}
