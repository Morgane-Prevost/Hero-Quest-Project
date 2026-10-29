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
/// Rounds of a quest (rulebook "Ordre du jeu"): the heroes play one after the other,
/// then Zargon moves all the monsters on the board, then a new round starts.
///
/// Hero turn: one move (2 red dice) and ONE action, in any order (move then action, or action then move).
/// Free things (looking, opening doors, potions, picking up) are not limited here.
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

    [Tooltip("Test keys (new Input System): M = move, A = attack, E = end hero turn. Disable in the real game.")]
    [SerializeField] private bool debugKeys = true;
    [SerializeField] private bool logTurns = true;

    private readonly List<Hero> heroOrder = new List<Hero>();
    private int heroIndex = -1;
    private bool busy;
    private bool waitingForActionResult;

    private Coroutine moveCoroutine;
    private GridPawn movePawn;
    private Action<GridPawn, Vector2Int, Vector2Int> moveHandler;

    public TurnPhase Phase { get; private set; } = TurnPhase.NotStarted;
    public int Round { get; private set; }
    public Hero CurrentHero { get; private set; }
    public bool HasMoved { get; private set; }
    public bool HasActed { get; private set; }
    public bool IsBusy => busy;

    public bool CanMove => Phase == TurnPhase.HeroTurn && !busy && !HasMoved;
    public bool CanAct => Phase == TurnPhase.HeroTurn && !busy && !HasActed;
    public bool CanEndTurn => Phase == TurnPhase.HeroTurn && !busy;

    public event Action<int> RoundStarted;
    public event Action<Hero> HeroTurnStarted;
    public event Action<Hero> HeroTurnEnded;
    public event Action ZargonTurnStarted;
    public event Action ZargonTurnEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // A trap card can end the current hero's turn (treasure search).
        if (HeroQuestWorldBridge.Instance != null)
            HeroQuestWorldBridge.Instance.HeroTurnEndRequested += OnHeroTurnEndRequested;
    }

    private void OnDestroy()
    {
        if (HeroQuestWorldBridge.Instance != null)
            HeroQuestWorldBridge.Instance.HeroTurnEndRequested -= OnHeroTurnEndRequested;
    }

    // ───────── Quest ─────────

    // order = heroes in play order (the hero to Zargon's left first). Null = the order of HeroManager.
    public void StartQuest(IEnumerable<Hero> order = null)
    {
        StopQuest();

        heroOrder.Clear();
        if (order != null) heroOrder.AddRange(order);
        else heroOrder.AddRange(GatherHeroes());

        if (!AnyActiveHero())
        {
            Debug.LogError("[Turn] No hero to play: the heroes must exist before the rounds start.");
            return;
        }

        Phase = TurnPhase.NotStarted;
        busy = false;
        waitingForActionResult = false;
        Round = 0;
        BeginRound();
    }

    // Called by GameplayLoop when QuestManager ends the quest. Stops everything in progress.
    public void StopQuest()
    {
        if (Phase == TurnPhase.QuestEnded && !busy) return;

        StopMoveRoutine();
        if (CellPicker.Instance != null && CellPicker.Instance.IsPicking) CellPicker.Instance.Cancel();
        if (monsterExecutor != null) monsterExecutor.TurnFinished -= OnMonsterTurnFinished;

        busy = false;
        waitingForActionResult = false;
        CurrentHero = null;
        Phase = TurnPhase.QuestEnded;
        Log("Rounds stopped.");
    }

    // ───────── Rounds ─────────

    private void BeginRound()
    {
        if (Phase == TurnPhase.QuestEnded) return;

        Round++;
        heroIndex = -1;
        Log("=== Round " + Round + " ===");
        RoundStarted?.Invoke(Round);

        // Quest rules based on turns ("after 10 turns..."). May end the quest (-> StopQuest).
        QuestEvents.Raise(QuestEventType.TurnStarted);
        if (Phase == TurnPhase.QuestEnded) return;

        NextHero();
    }

    private void NextHero()
    {
        if (Phase == TurnPhase.QuestEnded) return;

        if (!AnyActiveHero())
        {
            // QuestManager should already have ended the quest (all dead / escaped). Stop here to avoid looping.
            Debug.LogWarning("[Turn] No active hero left but the quest is not ended: rounds stopped.");
            StopQuest();
            return;
        }

        while (++heroIndex < heroOrder.Count)
        {
            Hero hero = heroOrder[heroIndex];
            if (IsActiveHero(hero))
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
        HeroTurnStarted?.Invoke(hero);
    }

    // Roll the movement dice and let the hero choose a cell. One move per turn.
    public bool RequestMove()
    {
        if (!CurrentHeroCanPlay() || !CanMove) return false;
        moveCoroutine = StartCoroutine(MoveRoutine(CurrentHero));
        return true;
    }

    private IEnumerator MoveRoutine(Hero hero)
    {
        GridPawn pawn = hero.GetComponent<GridPawn>();
        if (pawn == null)
        {
            Debug.LogError("[Turn] " + hero.heroName + " has no GridPawn.");
            moveCoroutine = null;
            yield break;
        }

        busy = true;
        bool moved = false;
        moveHandler = (p, from, to) => moved = true;
        movePawn = pawn;
        pawn.CellChanged += moveHandler;

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

        StopMoveRoutine();
        if (moved) HasMoved = true;
        else Log(hero.heroName + " cancelled the move (can still move).");
        busy = false;
    }

    private void StopMoveRoutine()
    {
        if (moveCoroutine != null) { StopCoroutine(moveCoroutine); moveCoroutine = null; }
        if (movePawn != null && moveHandler != null) movePawn.CellChanged -= moveHandler;
        movePawn = null;
        moveHandler = null;
    }

    // One action per turn. Calls the matching Hero method.
    public bool RequestAction(HeroActionType action)
    {
        if (!CurrentHeroCanPlay() || !CanAct) return false;
        Hero hero = CurrentHero;

        if (heroReportsActionResult)
        {
            // Set BEFORE calling the hero, in case it answers immediately.
            busy = true;
            waitingForActionResult = true;
        }

        switch (action)
        {
            case HeroActionType.Attack: hero.AttackAction(); break;
            case HeroActionType.CastSpell: hero.SpellAction(); break;
            case HeroActionType.SearchTreasure: hero.LookForTreasureAction(); break;
            case HeroActionType.SearchSecretDoors: hero.LookForSecretDoorAction(); break;
            case HeroActionType.SearchTraps: hero.LookForTrapAction(); break;
            case HeroActionType.DisarmTrap: hero.DisarmTrapAction(); break;
        }

        if (!heroReportsActionResult) HasActed = true;
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
        FinishHeroTurn();
    }

    // The treasure trap card ends the turn even if an action is still "in progress".
    private void OnHeroTurnEndRequested(Hero hero)
    {
        if (Phase != TurnPhase.HeroTurn || hero != CurrentHero) return;
        StopMoveRoutine();
        busy = false;
        waitingForActionResult = false;
        Log(hero.heroName + " turn ended by a card.");
        FinishHeroTurn();
    }

    private void FinishHeroTurn()
    {
        Hero ended = CurrentHero;
        CurrentHero = null;
        Log("End of hero turn: " + (ended != null ? ended.heroName : "?"));
        HeroTurnEnded?.Invoke(ended);
        NextHero();
    }

    // If the current hero died during his own turn (trap...), his turn ends by itself.
    private bool CurrentHeroCanPlay()
    {
        if (Phase != TurnPhase.HeroTurn || CurrentHero == null) return false;
        if (!IsActiveHero(CurrentHero))
        {
            StopMoveRoutine();
            busy = false;
            FinishHeroTurn();
            return false;
        }
        return true;
    }

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
        if (Phase == TurnPhase.QuestEnded) return; // the quest ended during the monsters' turn
        ZargonTurnEnded?.Invoke();
        BeginRound();
    }

    // ───────── Helpers ─────────

    // Allies are moved by their controller, escaped / dead heroes do not play.
    private static bool IsActiveHero(Hero hero)
    {
        return hero != null && !hero.IsAlly && !hero.IsDead && !hero.HasEscaped;
    }

    private bool AnyActiveHero()
    {
        foreach (Hero hero in heroOrder)
            if (IsActiveHero(hero)) return true;
        return false;
    }

    private static List<Hero> GatherHeroes()
    {
        List<Hero> list = new List<Hero>();
        if (HeroManager.Instance != null)
            foreach (Hero hero in HeroManager.Instance.Heroes)
                if (hero != null) list.Add(hero);
        return list;
    }

    private void Log(string message)
    {
        if (logTurns) Debug.Log("[Turn] " + message);
    }

#if ENABLE_INPUT_SYSTEM
    private void Update()
    {
        if (!debugKeys || Keyboard.current == null) return;

        if (Keyboard.current.mKey.wasPressedThisFrame) RequestMove();
        if (Keyboard.current.aKey.wasPressedThisFrame) RequestAction(HeroActionType.Attack);
        if (Keyboard.current.eKey.wasPressedThisFrame) EndHeroTurn();
    }
#endif
}