using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HeroQuest.Quests;
using System.Text;

public class QuestPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleButtonText;
    [SerializeField] private GameObject questBox;

    [Header("Text UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Other")]
    [SerializeField] private ListPanel listPanel;
    [SerializeField] private InventoryPanel inventoryPanel;

    private void Awake()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(OnToggle);

        if (questBox != null)
            questBox.SetActive(false);

        if (toggleButtonText != null)
            toggleButtonText.text = "+";
    }

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += SetQuest;

            if (QuestManager.Instance.Context != null && QuestManager.Instance.Context.Quest != null)
            {
                SetQuest(QuestManager.Instance.Context.Quest);
            }
        }
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= SetQuest;
        }
    }

    public void SetQuest(QuestData quest)
    {
        if (quest == null) return;

        // 1. Titre fixe en haut du panneau
        if (titleText != null)
        {
            titleText.text = "Quest in progress"; // Ou laisse vide cette ligne si c'est déjà écrit dans Unity
        }

        // 2. Contenu textuel dans la description
        if (descriptionText != null)
        {
            StringBuilder sb = new StringBuilder();

            // Titre spécifique de la quête (ex: "The Trial")
            if (!string.IsNullOrEmpty(quest.title))
            {
                sb.AppendLine($"<b><size=115%>{quest.title}</size></b>");
                sb.AppendLine();
            }

            // Description principale
            if (!string.IsNullOrEmpty(quest.description))
            {
                sb.AppendLine(quest.description);
                sb.AppendLine();
            }

            // Note d'introduction
            if (!string.IsNullOrEmpty(quest.introNote))
            {
                string header = string.IsNullOrEmpty(quest.introTitle) ? "Quête" : quest.introTitle;
                sb.AppendLine($"<b>{header} :</b> {quest.introNote}");
                sb.AppendLine();
            }

            // Objectifs
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
    }

    private void OnToggle()
    {
        if (questBox == null) return;

        bool isOpen = !questBox.activeSelf;
        questBox.SetActive(isOpen);

        if (isOpen)
        {
            if (toggleButtonText != null) toggleButtonText.text = "-";
            if (listPanel != null) listPanel.ClearSelection();
            if (inventoryPanel != null) inventoryPanel.Close();
        }
        else
        {
            if (toggleButtonText != null) toggleButtonText.text = "+";
        }
    }

    public void Close()
    {
        if (questBox != null) questBox.SetActive(false);
        if (toggleButtonText != null) toggleButtonText.text = "+";
    }
}