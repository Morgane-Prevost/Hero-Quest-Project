using UnityEngine;
using UnityEngine.InputSystem;

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
    public event System.Action<string> Rolled;


    public InputActionReference leftClick;
    private IRandomPicker picker;
    public void Initialize(IRandomPicker picker) => this.picker = picker;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (picker == null) picker = new SystemRandomPicker();
    }

    private void OnEnable()
    {
        leftClick.action.Enable();
    }

    private void OnDisable()
    {
        leftClick.action.Disable();
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
        int result = picker.Pick(6) + 1;
        WhiteDiceFaces face = result <= 3 ? WhiteDiceFaces.Skull
                            : result <= 5 ? WhiteDiceFaces.WhiteShield
                            : WhiteDiceFaces.BlackShield;
        Rolled?.Invoke("Combat die: " + face);
        return face;
    }

    public int RollRedDice()
    {
        int result = picker.Pick(6) + 1;
        Rolled?.Invoke("Red die: " + result);
        return result;
    }
}
