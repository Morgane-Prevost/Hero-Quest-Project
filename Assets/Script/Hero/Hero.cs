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
}
