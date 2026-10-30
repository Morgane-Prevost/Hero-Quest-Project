using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HeroQuest.Quests;

public class QuestPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleButtonText;
    [SerializeField] private GameObject questBox;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Other")]
    [SerializeField] private ListPanel listPanel;
    [SerializeField] private InventoryPanel inventoryPanel;

    private bool subscribed;

    private void Awake()
    {
        toggleButton.onClick.AddListener(OnToggle);

        questBox.SetActive(false);
        toggleButtonText.text = "+";
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
        if (subscribed && QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= SetQuest;
        }

        subscribed = false;
    }

    private void Subscribe()
    {
        if (subscribed || QuestManager.Instance == null)
            return;

        QuestManager.Instance.OnQuestStarted += SetQuest;
        subscribed = true;

        if (QuestManager.Instance.Context != null)
        {
            SetQuest(QuestManager.Instance.Context.Quest);
        }
    }

    public void SetQuest(QuestData quest)
    {
        if (quest == null)
            return;

        descriptionText.text = quest.description;
    }

    private void OnToggle()
    {
        bool isOpen = !questBox.activeSelf;
        questBox.SetActive(isOpen);

        if (isOpen)
        {
            toggleButtonText.text = "-";
            listPanel.ClearSelection();
            inventoryPanel.Close();
        }
        else
        {
            toggleButtonText.text = "+";
        }
    }

    public void Close()
    {
        questBox.SetActive(false);
        toggleButtonText.text = "+";
    }
}