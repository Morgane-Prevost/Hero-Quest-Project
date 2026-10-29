using HeroQuest.Quests;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Boot,
    MainMenu,
    QuestSelect,
    QuestPlaying,   // TurnManager runs the rounds
    QuestResult,    // victory / defeat screen
    BetweenQuests   // gold, equipment (armory), save
}

/// <summary>
/// level loop of the whole game:
///   MainMenu -> QuestSelect -> QuestPlaying (rounds: heroes, then Zargon) -> QuestResult -> BetweenQuests -> QuestSelect ...
///
/// It owns NO rule. It only calls the other systems in the right order:
///   QuestManager  : loads the level, places the heroes, runs the quest rules, decides victory / defeat.
///   TurnManager   : heroes' turns, then Zargon's monsters, round after round.
/// </summary>
public class GameplayLoop : MonoBehaviour
{
    public static GameplayLoop Instance { get; private set; }

    [Tooltip("Campaign order. A quest is unlocked when the previous one has been won.")]
    [SerializeField] private List<QuestData> quests = new List<QuestData>();

    [Tooltip("TEST SHORTCUT: if assigned, the menus are skipped and this quest starts when the scene starts.")]
    [SerializeField] private QuestData autoStartQuest;

    [SerializeField] private bool logFlow = true;

    private readonly HashSet<string> completedQuestIds = new HashSet<string>(); // in memory only (no save yet)
    private bool abandoning;

    public GameState State { get; private set; } = GameState.Boot;
    public QuestData SelectedQuest { get; private set; }
    public QuestData CurrentQuest { get; private set; }
    public QuestResult LastResult { get; private set; }
    public IReadOnlyList<QuestData> Quests => quests;

    public event Action<GameState> StateChanged;
    public event Action<QuestData, QuestResult> QuestFinished;

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
        if (QuestManager.Instance == null || TurnManager.Instance == null)
        {
            Debug.LogError("[Flow] QuestManager and TurnManager must be in the scene.");
            return;
        }

        QuestManager.Instance.OnQuestEnded += OnQuestEnded;

        if (autoStartQuest != null)
        {
            SelectedQuest = autoStartQuest;
            StartSelectedQuest();
        }
        else
        {
            SetState(GameState.MainMenu);
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null) QuestManager.Instance.OnQuestEnded -= OnQuestEnded;
    }

    // ───────── Menus (called by the UI) ─────────

    public void StartGame()
    {
        if (State != GameState.MainMenu) return;
        SetState(GameState.QuestSelect);
    }

    public bool IsCompleted(QuestData quest)
    {
        return quest != null && completedQuestIds.Contains(quest.questId);
    }

    public bool IsUnlocked(QuestData quest)
    {
        int index = quests.IndexOf(quest);
        if (index < 0) return false;
        return index == 0 || IsCompleted(quests[index - 1]);
    }

    public bool SelectQuest(QuestData quest)
    {
        if (State != GameState.QuestSelect) return false;
        if (!IsUnlocked(quest))
        {
            Debug.LogWarning("[Flow] Quest locked: " + (quest != null ? quest.title : "null"));
            return false;
        }
        SelectedQuest = quest;
        return true;
    }

    // ───────── Playing ─────────

    public void StartSelectedQuest()
    {
        if (State != GameState.QuestSelect && State != GameState.Boot) return;
        if (SelectedQuest == null) { Debug.LogWarning("[Flow] No quest selected."); return; }

        CurrentQuest = SelectedQuest;
        abandoning = false;
        SetState(GameState.QuestPlaying);

        // 1. Quest: loads the level, places (or creates) the heroes, raises QuestStarted.
        QuestManager.Instance.StartQuest(CurrentQuest);
        if (!QuestManager.Instance.IsRunning)
        {
            Debug.LogError("[Flow] The quest did not start (see the errors above).");
            SetState(GameState.QuestSelect);
            return;
        }

        // 2. Rounds: heroes, then Zargon, again and again until the quest ends.
        TurnManager.Instance.StartQuest();
    }

    // Quit a quest in progress (pause menu). No result screen, no reward.
    public void AbandonQuest()
    {
        if (State != GameState.QuestPlaying) return;
        abandoning = true;
        QuestManager.Instance.ForceEnd(QuestResult.Defeat); // -> OnQuestEnded
    }

    private void OnQuestEnded(QuestResult result, QuestData quest)
    {
        TurnManager.Instance.StopQuest();
        LastResult = result;

        if (abandoning)
        {
            abandoning = false;
            SetState(GameState.MainMenu);
            return;
        }

        if (result == QuestResult.Victory && quest != null) completedQuestIds.Add(quest.questId);
        Log("Quest ended: " + result);
        QuestFinished?.Invoke(quest, result);
        SetState(GameState.QuestResult);
    }

    // ───────── After the quest ─────────

    // From the result screen. Gold and equipment are handled in BetweenQuests (not built yet).
    public void ContinueAfterResult()
    {
        if (State != GameState.QuestResult) return;
        SetState(GameState.BetweenQuests);
    }

    public void LeaveBetweenQuests()
    {
        if (State != GameState.BetweenQuests) return;
        SelectedQuest = null;
        SetState(GameState.QuestSelect);
    }

    public void BackToMainMenu()
    {
        if (State == GameState.QuestPlaying) { AbandonQuest(); return; }
        SetState(GameState.MainMenu);
    }

    // ───────── Helpers ─────────

    private void SetState(GameState state)
    {
        if (State == state) return;
        State = state;
        Log("State: " + state);
        StateChanged?.Invoke(state);
    }

    private void Log(string message)
    {
        if (logFlow) Debug.Log("[Flow] " + message);
    }
}