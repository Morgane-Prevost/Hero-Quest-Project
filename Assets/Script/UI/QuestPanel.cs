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

    [Header("HideUI")]
    [SerializeField] private GameObject[] uiToHide;

    [Header("Other")]
    [SerializeField] private ListPanel listPanel;
    [SerializeField] private InventoryPanel inventoryPanel;

    private bool subscribed;

    private void Awake()
    {
        if (box != null) box.SetActive(false);
        if (okButton != null) okButton.onClick.AddListener(Close);
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

        if (QuestManager.Instance.Context != null && QuestManager.Instance.Context.Quest != null)
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
        if (alreadyShown) return;

        alreadyShown = true;

        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(quest.title)
                ? $"Quête {quest.questNumber}"
                : $"Quête {quest.questNumber} : {quest.title}";
        }

        if (descriptionText != null)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(quest.description))
            {
                sb.AppendLine(quest.description);
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(quest.introNote))
            {
                string header = string.IsNullOrEmpty(quest.introTitle) ? "Note" : quest.introTitle;
                sb.AppendLine($"<b>{header} :</b> {quest.introNote}");
                sb.AppendLine();
            }

            if (quest.objectives != null && quest.objectives.Count > 0)
            {
                sb.AppendLine("<b>Objectifs :</b>");
                foreach (var obj in quest.objectives)
                {
                    string optionalTag = obj.optional ? " (Optionnel)" : "";
                    sb.AppendLine($"• {obj.description}{optionalTag}");
                }
            }

            descriptionText.text = sb.ToString();
        }

        SetOtherUIVisible(false);

        if (box != null) box.SetActive(true);
    }

    public void Close()
    {
        if (box != null) box.SetActive(false);

        SetOtherUIVisible(true);
    }

    private void SetOtherUIVisible(bool visible)
    {
        if (uiToHide != null && uiToHide.Length > 0)
        {
            foreach (var element in uiToHide)
            {
                if (element != null && element != gameObject)
                {
                    element.SetActive(visible);
                }
            }
        }
        else if (transform.parent != null)
        {
            foreach (Transform child in transform.parent)
            {
                if (child.gameObject != gameObject && child.gameObject != box)
                {
                    child.gameObject.SetActive(visible);
                }
            }
        }
    }
}