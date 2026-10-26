using UnityEngine;

public static class Effect 
{
    public static void speedPotionEffect(Hero hero)
    {
        hero.movementDices += 2;
    }

    public static void blessedWaterEffect() //Has to take a monster as parameter and kill it if it s an undead monster
    {
        //Call Kill function for the monster
    }
}
