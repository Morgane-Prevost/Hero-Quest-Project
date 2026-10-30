using System.Collections.Generic;
using HeroQuest.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Spell group choice at the start of a quest (rulebook p.8). The panel is built by hand in the scene.
// Put this script on an ACTIVE object (not on the panel itself, which starts hidden).
public class SpellDraftUI : MonoBehaviour
{
    [SerializeField] private List<SpellCardSO> allSpells;      // the 12 hero spell assets

    [Header("Panel (built by hand)")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [Tooltip("In this order: Air, Fire, Water, Earth")]
    [SerializeField] private Button[] elementButtons = new Button[4];
    [Tooltip("Optional: the rulebook suggestion for a first quest")]
    [SerializeField] private Button suggestionButton;

    private SpellDraft draft;
    private Hero wizard, elf;
    private bool subscribed;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);

        for (int i = 0; i < elementButtons.Length; i++)
        {
            if (elementButtons[i] == null) continue;
            SpellElement element = (SpellElement)i;
            elementButtons[i].onClick.AddListener(() => OnElementClicked(element));
        }
        if (suggestionButton != null) suggestionButton.onClick.AddListener(OnSuggestionClicked);

        if (QuestManager.Instance == null) { Debug.LogError("[Draft] No QuestManager in the scene."); return; }
        QuestManager.Instance.OnQuestStarted += OnQuestStarted;
        subscribed = true;
    }

    private void OnDestroy()
    {
        if (subscribed && QuestManager.Instance != null) QuestManager.Instance.OnQuestStarted -= OnQuestStarted;
    }

    // Heroes already exist here (PlaceHeroes runs before OnQuestStarted)
    private void OnQuestStarted(QuestData quest)
    {
        if (SpellManager.Instance == null) { Debug.LogError("[Draft] No SpellManager in the scene."); return; }

        wizard = HeroManager.Instance.GetByType(HeroType.Wizard);
        elf = HeroManager.Instance.GetByType(HeroType.Elf);
        if (wizard == null || elf == null)
        {
            Debug.LogWarning("[Draft] Needs both the Wizard and the Elf in HeroManager: spell choice skipped.");
            return;
        }

        try { draft = new SpellDraft(allSpells); }
        catch (System.ArgumentException e) { Debug.LogError("[Draft] " + e.Message); return; }

        if (TurnManager.Instance != null) TurnManager.Instance.Locked = true;
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    private void OnElementClicked(SpellElement element)
    {
        if (draft == null || !draft.Pick(element)) return;
        if (draft.IsDone) Finish(); else Refresh();
    }

    // Rulebook suggestion for a first quest: Wizard fire + water + air, Elf earth
    private void OnSuggestionClicked()
    {
        if (draft == null || !draft.IsWizardTurn) return;
        draft.Pick(SpellElement.Fire);
        draft.Pick(SpellElement.Earth);
        Finish();
    }

    private void Refresh()
    {
        if (titleText != null)
            titleText.text = draft.IsWizardTurn ? "Wizard: choose your first spell group"
                                                : "Elf: choose your spell group";

        for (int i = 0; i < elementButtons.Length; i++)
        {
            if (elementButtons[i] == null) continue;
            SpellElement element = (SpellElement)i;
            elementButtons[i].gameObject.SetActive(draft.IsAvailable(element));

            TMP_Text label = elementButtons[i].GetComponentInChildren<TMP_Text>();
            if (label == null) continue;
            var names = new List<string>();
            foreach (SpellCardSO spell in draft.Group(element)) names.Add(spell.displayName);
            label.text = element + ": " + string.Join(", ", names);
        }

        if (suggestionButton != null) suggestionButton.gameObject.SetActive(draft.IsWizardTurn);
    }

    private void Finish()
    {
        SpellManager.Instance.AssignSpells(wizard, draft.WizardSpells);
        SpellManager.Instance.AssignSpells(elf, draft.ElfSpells);
        Debug.Log("[Draft] Wizard: " + draft.WizardSpells.Count + " spells, Elf: " + draft.ElfSpells.Count + " spells.");

        draft = null;
        if (panel != null) panel.SetActive(false);
        if (TurnManager.Instance != null) TurnManager.Instance.Locked = false;
    }
}