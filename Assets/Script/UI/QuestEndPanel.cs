using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using HeroQuest.Quests;

public class QuestEndPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject box;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;

    [Header("Data")]
    [SerializeField] private QuestCatalog catalog;

    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string menuSceneName = "MainMenu";

    private bool subscribed;
    private QuestData nextQuest;

    private void Awake()
    {
        box.SetActive(false);

        nextButton.onClick.AddListener(OnNextClicked);
        retryButton.onClick.AddListener(OnRetryClicked);
        menuButton.onClick.AddListener(OnMenuClicked);
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (subscribed && TurnManager.Instance != null)
        {
            TurnManager.Instance.QuestFinished -= OnQuestFinished;
        }

        subscribed = false;
    }

    private void Subscribe()
    {
        if (subscribed || TurnManager.Instance == null)
            return;

        TurnManager.Instance.QuestFinished += OnQuestFinished;
        subscribed = true;
    }

    private void OnQuestFinished(bool heroesWon)
    {
        QuestData quest = GameSession.CurrentQuest;

        box.SetActive(true);

        if (heroesWon)
        {
            nextQuest = catalog.GetNext(quest);

            titleText.text = "Victory !";
            retryButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(nextQuest != null);

            if (nextQuest != null)
            {
                messageText.text = "Next quest : " + nextQuest.title;
            }
            else
            {
                messageText.text = "You finished all the quests !";
            }
        }
        else
        {
            titleText.text = "Defeat...";
            messageText.text = "Your heroes have fallen.";
            nextButton.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(true);
        }
    }

    private void OnNextClicked()
    {
        GameSession.CurrentQuest = nextQuest;
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnRetryClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnMenuClicked()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}