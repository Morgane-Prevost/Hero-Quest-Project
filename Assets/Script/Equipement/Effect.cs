using UnityEngine;

public class Effect : MonoBehaviour
{
    public void speedPotionEffect(Hero hero)
    {
        hero.movementDices += 2;
    }

    public void blessedWaterEffect() //Has to take a monster as parameter and kill it if it s an undead monster
    {
        //Call Kill function for the monster
    }
}
