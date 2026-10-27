using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeroQuest.Quests.UI
{
    /// <summary>Menu de départ : liste des quêtes + description affichée dès la sélection.</summary>
    public class QuestSelectionMenu : MonoBehaviour
    {
        [SerializeField] List<QuestData> quests = new();

        [Header("Liste")]
        [SerializeField] Transform listRoot;
        [SerializeField] Button questButtonPrefab;      // contient un TMP_Text enfant

        [Header("Détails (affichés à la sélection)")]
        [SerializeField] TMP_Text numberText;      // "QUÊTE 3" (optionnel)
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] Image thumbnail;
        [SerializeField] Button startButton;

        [SerializeField] GameObject menuRoot;

        QuestData selected;

        void Start()
        {
            foreach (var q in quests)
            {
                var btn = Instantiate(questButtonPrefab, listRoot);
                btn.GetComponentInChildren<TMP_Text>().text = $"{q.questNumber}. {q.title}";
                var captured = q;
                btn.onClick.AddListener(() => Select(captured));
            }

            startButton.onClick.AddListener(OnStart);
            ClearDetails();
            if (quests.Count > 0) Select(quests[0]);
        }

        void Select(QuestData q)
        {
            selected = q;
            if (numberText != null) numberText.text = $"QUÊTE {q.questNumber}";
            titleText.text = q.title;
            descriptionText.text = q.description;      // ← description affichée ici
            if (thumbnail != null) { thumbnail.sprite = q.thumbnail; thumbnail.enabled = q.thumbnail != null; }
            startButton.interactable = true;
        }

        void ClearDetails()
        {
            titleText.text = descriptionText.text = "";
            startButton.interactable = false;
        }

        void OnStart()
        {
            if (selected == null) return;
            menuRoot.SetActive(false);
            QuestManager.Instance.StartQuest(selected);
        }
    }
}
