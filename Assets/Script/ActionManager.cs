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

    public static ActionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int Fight(Hero hero, Monster monster, bool heroIsAttacking)
    {
        int finalResult = 0;

        if (heroIsAttacking)
        {
            for (int i = 0; i < hero.attackDices; i++)
            {
                WhiteDiceFaces result = RollWhiteDice();

                if (result == WhiteDiceFaces.Skull)
                {
                        finalResult += 1;
                }
            }
            for (int i = 0; i < monster.defenseDices; i++)
            {
                WhiteDiceFaces result = RollWhiteDice();

                if (result == WhiteDiceFaces.BlackShield)
                {
                    finalResult -= 1;
                }
            }
            return finalResult > 0 ? finalResult : 0;
        }
        else
        {
            for (int i = 0; i < monster.attackDices; i++)
            {
                WhiteDiceFaces result = RollWhiteDice();

                if (result == WhiteDiceFaces.Skull)
                {
                    finalResult += 1;
                }
            }
            for (int i = 0; i < hero.defenseDices; i++)
            {
                WhiteDiceFaces result = RollWhiteDice();

                if (result == WhiteDiceFaces.WhiteShield)
                {
                    finalResult -= 1;
                }
            }
            return finalResult > 0 ? finalResult : 0;
        }
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
