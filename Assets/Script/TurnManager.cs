using HeroQuest.Board;
using HeroQuest.Quests;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum TurnPhase
{
    NotStarted,
    HeroTurn,
    ZargonTurn,
    QuestEnded
}

// The six actions a hero can do in a turn (rulebook). One per turn.
public enum HeroActionType
{
    Attack,
    CastSpell,
    SearchTreasure,
    SearchSecretDoors,
    SearchTraps,
    DisarmTrap
}

/// <summary>
/// Turn by turn flow of a quest:
/// heroes play one after the other, then Zargon moves all the monsters on the board, then a new round starts.
///
/// Hero turn: one move (2 red dice) and ONE action, in any order (move then action, or action then move).
/// Free things (looking, opening doors, potions, picking up) are not handled here: they are always allowed.
///
/// The UI only needs: RequestMove(), RequestAction(type), EndHeroTurn() and the events below.
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [SerializeField] private MonsterActionExecutor monsterExecutor;

    [Tooltip("True: the monsters play automatically. False: a human Zargon must call EndZargonTurn() (not implemented yet).")]
    [SerializeField] private bool zargonIsAI = true;

    [Tooltip("False (for now): an action is counted as used as soon as it is requested, even if the player cancels it. " +
             "True: the hero code must call NotifyActionFinished(performed) when its action is over.")]
    [SerializeField] private bool heroReportsActionResult = false;

    [Tooltip("Test keys (new Input System): Enter = start quest, M = move, A = attack, E = end hero turn. Disable in the real game.")]
    [SerializeField] private bool debugKeys = true;
    [SerializeField] private bool logTurns = true;

    private readonly List<Hero> heroOrder = new List<Hero>();
    private int heroIndex = -1;
    private bool busy;
    private bool waitingForActionResult;

    public TurnPhase Phase { get; private set; } = TurnPhase.NotStarted;
    public int Round { get; private set; }
    public Hero CurrentHero { get; private set; }
    public bool HasMoved { get; private set; }
    public bool HasActed { get; private set; }
    public bool IsBusy => busy;

    // What the UI should enable.
    public bool Locked { get; set; }   // set by modal screens (spell choice): no input until released

    public bool CanMove => !Locked && Phase == TurnPhase.HeroTurn && !busy && !HasMoved && (CurrentHero == null || CurrentHero.CanMoveOrAttack);
    public bool CanAct => !Locked && Phase == TurnPhase.HeroTurn && !busy && !HasActed;
    public bool CanEndTurn => !Locked && Phase == TurnPhase.HeroTurn && !busy;

    public event Action<int> RoundStarted;
    public event Action<Hero> HeroTurnStarted;
    public event Action<Hero> HeroTurnEnded;
    public event Action ZargonTurnStarted;
    public event Action ZargonTurnEnded;
    public event Action<bool> QuestFinished; // true = heroes won

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this; // no DontDestroyOnLoad: this object references scene objects
    }


    private void Start()
    {
        if (HeroQuestWorldBridge.Instance != null)
            HeroQuestWorldBridge.Instance.HeroTurnEndRequested += OnTurnEndRequested;

        QuestManager quests = QuestManager.Instance;
        if (quests == null) return;
        quests.OnQuestStarted += OnQuestStarted;
        quests.OnQuestEnded += OnQuestEnded;

        // The quest may already be running if it was started before this Start()
        if (quests.IsRunning && Phase == TurnPhase.NotStarted) StartQuest(PlayableHeroes());
    }
    private void OnDestroy()
    {
        if (HeroQuestWorldBridge.Instance != null)
            HeroQuestWorldBridge.Instance.HeroTurnEndRequested -= OnTurnEndRequested;
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= OnQuestStarted;
            QuestManager.Instance.OnQuestEnded -= OnQuestEnded;
        }
    }

    private void OnQuestStarted(HeroQuest.Quests.QuestData quest)
    {
        if (Phase == TurnPhase.NotStarted || Phase == TurnPhase.QuestEnded) StartQuest(PlayableHeroes());
    }

    private void OnQuestEnded(HeroQuest.Quests.QuestResult result, HeroQuest.Quests.QuestData quest)
        => EndQuest(result == HeroQuest.Quests.QuestResult.Victory);

    private void OnTurnEndRequested(Hero hero)
    {
        if (Phase == TurnPhase.HeroTurn && hero == CurrentHero) EndHeroTurn();
    }

    private static List<Hero> PlayableHeroes()
    {
        var list = new List<Hero>();
        if (HeroManager.Instance == null) return list;
        foreach (Hero h in HeroManager.Instance.Heroes)
            if (h != null && !h.IsAlly) list.Add(h);
        return list;
    }

    // ───────── Quest ─────────

    // order = heroes in play order (the hero to Zargon's left first). Null = every Hero found in the scene.
    public void StartQuest(IEnumerable<Hero> order = null)
    {
        heroOrder.Clear();
        if (order != null) heroOrder.AddRange(order);
        else heroOrder.AddRange(FindHeroes());

        if (heroOrder.Count == 0)
        {
            Debug.LogError("[Turn] No hero to play: create the heroes before starting the quest.");
            return;
        }

        busy = false;
        waitingForActionResult = false;
        Round = 0;
        BeginRound();
    }

    // For quest conditions (objective reached, boss dead...). All heroes dead is detected automatically.
    public void EndQuest(bool heroesWon)
    {
        if (Phase == TurnPhase.QuestEnded) return;
        Phase = TurnPhase.QuestEnded;
        CurrentHero = null;
        Log("Quest finished: " + (heroesWon ? "heroes win" : "heroes lose"));
        QuestFinished?.Invoke(heroesWon);
    }

    // ───────── Rounds ─────────

    private void BeginRound()
    {
        Round++;
        heroIndex = -1;
        Log("=== Round " + Round + " ===");
        RoundStarted?.Invoke(Round);
        NextHero();
    }

    private void NextHero()
    {
        while (++heroIndex < heroOrder.Count)
        {
            Hero hero = heroOrder[heroIndex];
            if (IsAlive(hero))
            {
                StartHeroTurn(hero);
                return;
            }
        }
        StartZargonTurn(); // every hero has played
    }

    // ───────── Hero turn ─────────

    private void StartHeroTurn(Hero hero)
    {
        Phase = TurnPhase.HeroTurn;
        CurrentHero = hero;
        HasMoved = false;
        HasActed = false;
        busy = false;
        waitingForActionResult = false;
        Log("Hero turn: " + hero.heroName);
        QuestEvents.Raise(QuestEventType.TurnStarted);

        // Release rolls (sleep, control, horror) come first, so the UI sees the real state
        if (DreadSpellManager.Instance != null) DreadSpellManager.Instance.RollReleases(hero);
        HeroTurnStarted?.Invoke(hero);

        if (hero.HasEffect(HeroEffect.SkipTurn))   // tornado: the hero passes this turn
        {
            hero.EndEffect(HeroEffect.SkipTurn);
            Log(hero.heroName + " passes the turn (tornado).");
            EndHeroTurn();
        }
    }

    // Roll the movement dice and let the hero choose a cell. One move per turn.
    public bool RequestMove()
    {
        if (!CurrentHeroCanPlay() || !CanMove) return false;
        StartCoroutine(MoveRoutine(CurrentHero));
        return true;
    }

    private IEnumerator MoveRoutine(Hero hero)
    {
        GridPawn pawn = hero.GetComponent<GridPawn>();
        if (pawn == null)
        {
            Debug.LogError("[Turn] " + hero.heroName + " has no GridPawn.");
            yield break;
        }

        busy = true;
        bool moved = false;
        Action<GridPawn, Vector2Int, Vector2Int> onCellChanged = (p, from, to) => moved = true;
        pawn.CellChanged += onCellChanged;

        hero.Move();

        // The move is over when the pawn changes cell. If the player cancels the selection
        // (CellPicker stops without a move), a few frames of grace avoid mistaking "chosen" for "cancelled".
        int idleFrames = 0;
        while (!moved)
        {
            bool picking = CellPicker.Instance != null && CellPicker.Instance.IsPicking;
            idleFrames = picking ? 0 : idleFrames + 1;
            if (idleFrames > 3) break;
            yield return null;
        }

        pawn.CellChanged -= onCellChanged;
        if (moved) HasMoved = true;
        else Log(hero.heroName + " cancelled the move (can still move).");
        busy = false;
    }

    // One action per turn. Calls the matching Hero method.
    public bool RequestAction(HeroActionType action)
    {
        if (!CurrentHeroCanPlay() || !CanAct) return false;
        Hero hero = CurrentHero;
        if (action == HeroActionType.Attack && !hero.CanMoveOrAttack) return false;   // asleep or controlled

        if (heroReportsActionResult)
        {
            // Set BEFORE calling the hero, in case it answers immediately.
            busy = true;
            waitingForActionResult = true;
        }
        else if (action != HeroActionType.CastSpell) HasActed = true;
        else HasActed = true;

            switch (action)
            {
                case HeroActionType.Attack: hero.AttackAction(); break;
                case HeroActionType.CastSpell: hero.SpellAction(); break;
                case HeroActionType.SearchTreasure: hero.LookForTreasureAction(); break;
                case HeroActionType.SearchSecretDoors: hero.LookForSecretDoorAction(); break;
                case HeroActionType.SearchTraps: hero.LookForTrapAction(); break;
                case HeroActionType.DisarmTrap: hero.DisarmTrapAction(); break;
            }
        Log(hero.heroName + " action: " + action);
        return true;
    }

    // To be called by the hero code when its action is over (only if heroReportsActionResult is true).
    // performed = false when the player cancelled: the action is NOT used up.
    public void NotifyActionFinished(bool performed)
    {
        if (!waitingForActionResult) return;
        waitingForActionResult = false;
        busy = false;
        if (performed) HasActed = true;
    }

    public void EndHeroTurn()
    {
        if (Phase != TurnPhase.HeroTurn) return;
        if (busy)
        {
            Debug.LogWarning("[Turn] Cannot end the turn: a move or action is still in progress.");
            return;
        }

        Hero ended = CurrentHero;
        CurrentHero = null;
        Log("End of hero turn: " + (ended != null ? ended.heroName : "?"));
        HeroTurnEnded?.Invoke(ended);

        if (AllHeroesDead()) { EndQuest(false); return; }
        NextHero();
    }

    // If the current hero died during his own turn (trap...), his turn ends by itself.
    private bool CurrentHeroCanPlay()
    {
        if (Phase != TurnPhase.HeroTurn || CurrentHero == null) return false;
        if (!IsAlive(CurrentHero))
        {
            busy = false;
            EndHeroTurn();
            return false;
        }
        return true;
    }

    // Called by the spell menu once a spell is really cast
    public void MarkActionUsed() { HasActed = true; }

    // ───────── Zargon turn ─────────

    private void StartZargonTurn()
    {
        Phase = TurnPhase.ZargonTurn;
        CurrentHero = null;
        Log("Zargon's turn");
        ZargonTurnStarted?.Invoke();

        if (!zargonIsAI)
        {
            Debug.Log("[Turn] Human Zargon: call EndZargonTurn() when he is done (not implemented yet).");
            return;
        }

        if (monsterExecutor == null)
        {
            Debug.LogError("[Turn] MonsterActionExecutor not assigned: Zargon's turn is skipped.");
            FinishZargonTurn();
            return;
        }

        monsterExecutor.TurnFinished += OnMonsterTurnFinished;
        monsterExecutor.StartMonsterTurn();
    }

    private void OnMonsterTurnFinished()
    {
        monsterExecutor.TurnFinished -= OnMonsterTurnFinished;
        FinishZargonTurn();
    }

    public void EndZargonTurn()
    {
        if (Phase != TurnPhase.ZargonTurn || zargonIsAI) return;
        FinishZargonTurn();
    }

    private void FinishZargonTurn()
    {
        ZargonTurnEnded?.Invoke();
        if (AllHeroesDead()) { EndQuest(false); return; }
        BeginRound();
    }

    // ───────── Helpers ─────────

    private static bool IsAlive(Hero hero)
    {
        return hero != null && hero.currentBodyPoints > 0;
    }

    private bool AllHeroesDead()
    {
        foreach (Hero hero in heroOrder)
            if (IsAlive(hero)) return false;
        return true;
    }

    private static List<Hero> FindHeroes()
    {
#if UNITY_2023_1_OR_NEWER
        List<Hero> found = new List<Hero>(FindObjectsByType<Hero>(FindObjectsSortMode.None));
#else
        List<Hero> found = new List<Hero>(FindObjectsOfType<Hero>());
#endif
        // Placeholder order. Replace by the real seating order (hero to Zargon's left first).
        found.Sort((a, b) => string.CompareOrdinal(a.heroName, b.heroName));
        return found;
    }

    private void Log(string message)
    {
        if (logTurns) Debug.Log("[Turn] " + message);
    }

#if ENABLE_INPUT_SYSTEM
    private void Update()
    {
        if (!debugKeys || Keyboard.current == null) return;

        if (Keyboard.current.enterKey.wasPressedThisFrame && Phase == TurnPhase.NotStarted) StartQuest();
        if (Keyboard.current.mKey.wasPressedThisFrame) RequestMove();
        if (Keyboard.current.aKey.wasPressedThisFrame) RequestAction(HeroActionType.Attack);
        if (Keyboard.current.eKey.wasPressedThisFrame) EndHeroTurn();
    }
#endif
}