using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using HeroQuest.Quests;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject questPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backButton;

    [Header("Quests")]
    [SerializeField] private QuestCatalog catalog;
    [SerializeField] private Transform questList;
    [SerializeField] private Button questButtonPrefab;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "SampleScene";

    private void Awake()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        backButton.onClick.AddListener(OnBackClicked);

        CreateQuestButtons();

        mainPanel.SetActive(true);
        questPanel.SetActive(false);
    }

    private void CreateQuestButtons()
    {
        for (int i = 0; i < catalog.quests.Count; i++)
        {
            QuestData quest = catalog.quests[i];

            Button button = Instantiate(questButtonPrefab, questList);
            button.GetComponentInChildren<TMP_Text>().text = "Quest " + quest.questNumber + " : " + quest.title;

            button.onClick.AddListener(() => OnQuestClicked(quest));
        }
    }

    private void OnPlayClicked()
    {
        mainPanel.SetActive(false);
        questPanel.SetActive(true);
    }

    private void OnBackClicked()
    {
        questPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    private void OnQuestClicked(QuestData quest)
    {
        GameSession.CurrentQuest = quest;
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}