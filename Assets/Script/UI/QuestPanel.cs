using HeroQuest.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject box;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text hudTitleText;
    [SerializeField] private Button okButton;

    private bool subscribed = false;
    private bool alreadyShown = false;

    private void Awake()
    {
        box.SetActive(false);
        okButton.onClick.AddListener(Close);
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
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed) return;
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.OnQuestStarted += SetQuest;
        subscribed = true;

        if (QuestManager.Instance.Context != null)
        {
            SetQuest(QuestManager.Instance.Context.Quest);
        }
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (QuestManager.Instance != null) QuestManager.Instance.OnQuestStarted -= SetQuest;
        subscribed = false;
    }

    private void SetQuest(QuestData quest)
    {
        if (quest == null) return;

        if (hudTitleText != null)
        {
            hudTitleText.text = "Quest " + quest.questNumber + " : " + quest.title;
        }

        if (alreadyShown) return;

        alreadyShown = true;

        titleText.text = "Quest " + quest.questNumber + " : " + quest.title;
        descriptionText.text = quest.description;
        box.SetActive(true);
    }

    public void Close()
    {
        box.SetActive(false);
    }
}