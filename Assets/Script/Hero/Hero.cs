using UnityEngine;

public class Hero : MonoBehaviour
{
    public string heroName { get; set; }

    public int bodyPoints { get; set; }
    public int mindPoints { get; set; }
    public int attackDices { get; set; }
    public int defenseDices { get; set; }

    public int movementDices { get; set; }

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
