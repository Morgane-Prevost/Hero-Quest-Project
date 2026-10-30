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
    [Header("Dice textures")]
    [SerializeField] private Texture[] redDiceTextures;   // 6 textures : faces 1 à 6
    [SerializeField] private Texture skullTexture;
    [SerializeField] private Texture whiteShieldTexture;
    [SerializeField] private Texture blackShieldTexture;

    public DiceRollAnimator diceAnimator;
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
                WhiteDiceFaces result = RollWhiteDice(diceAnimator);

                if (result == WhiteDiceFaces.Skull)
                {
                        finalResult += 1;
                }
            }
            for (int i = 0; i < monster.defenseDices; i++)
            {
                WhiteDiceFaces result = RollWhiteDice(diceAnimator);

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
                WhiteDiceFaces result = RollWhiteDice(diceAnimator);

                if (result == WhiteDiceFaces.Skull)
                {
                    finalResult += 1;
                }
            }
            for (int i = 0; i < hero.defenseDices; i++)
            {
                WhiteDiceFaces result = RollWhiteDice(diceAnimator);

                if (result == WhiteDiceFaces.WhiteShield)
                {
                    finalResult -= 1;
                }
            }
            return finalResult > 0 ? finalResult : 0;
        }
    }

    public WhiteDiceFaces RollWhiteDice(DiceRollAnimator animator = null)
    {
        int result = picker.Pick(6) + 1;
        WhiteDiceFaces face = result <= 3 ? WhiteDiceFaces.Skull
                            : result <= 5 ? WhiteDiceFaces.WhiteShield
                            : WhiteDiceFaces.BlackShield;

        if (animator != null)
        {
            var allFaces = new[] { skullTexture, whiteShieldTexture, blackShieldTexture };
            animator.Play(allFaces, GetWhiteTexture(face));
        }

        Rolled?.Invoke("Combat die: " + face);
        return face;
    }

    public int RollRedDice(DiceRollAnimator animator = null)
    {
        int result = picker.Pick(6) + 1;

        if (animator != null)
            animator.Play(redDiceTextures, redDiceTextures[result - 1]);

        Rolled?.Invoke("Red die: " + result);
        return result;
    }

    private Texture GetWhiteTexture(WhiteDiceFaces face) => face switch
    {
        WhiteDiceFaces.Skull => skullTexture,
        WhiteDiceFaces.WhiteShield => whiteShieldTexture,
        _ => blackShieldTexture
    };
}
