using UnityEngine;
using UnityEngine.UI;

public class TurnButtons : MonoBehaviour
{
    [Header("Main Controls")]
    [SerializeField] private Button moveButton;
    [SerializeField] private Button endTurnButton;

    [Header("Actions")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button castSpellButton;
    [SerializeField] private Button searchTreasureButton;
    [SerializeField] private Button searchSecretDoorsButton;
    [SerializeField] private Button searchTrapsButton;
    [SerializeField] private Button disarmTrapButton;

    private void Awake()
    {
        if (moveButton != null) moveButton.onClick.AddListener(OnMoveClicked);
        if (endTurnButton != null) endTurnButton.onClick.AddListener(OnEndTurnClicked);

        if (attackButton != null) attackButton.onClick.AddListener(() => OnActionClicked(HeroActionType.Attack));
        if (castSpellButton != null) castSpellButton.onClick.AddListener(() => OnActionClicked(HeroActionType.CastSpell));
        if (searchTreasureButton != null) searchTreasureButton.onClick.AddListener(() => OnActionClicked(HeroActionType.SearchTreasure));
        if (searchSecretDoorsButton != null) searchSecretDoorsButton.onClick.AddListener(() => OnActionClicked(HeroActionType.SearchSecretDoors));
        if (searchTrapsButton != null) searchTrapsButton.onClick.AddListener(() => OnActionClicked(HeroActionType.SearchTraps));
        if (disarmTrapButton != null) disarmTrapButton.onClick.AddListener(() => OnActionClicked(HeroActionType.DisarmTrap));
    }

    private void OnMoveClicked()
    {
        if (TurnManager.Instance != null) TurnManager.Instance.RequestMove();
    }

    private void OnActionClicked(HeroActionType actionType)
    {
        if (TurnManager.Instance != null) TurnManager.Instance.RequestAction(actionType);
    }

    private void OnEndTurnClicked()
    {
        if (TurnManager.Instance != null) TurnManager.Instance.EndHeroTurn();
    }

    private void Update()
    {
        if (TurnManager.Instance == null) return;

        bool canMove = TurnManager.Instance.CanMove;
        bool canAct = TurnManager.Instance.CanAct;
        bool canEnd = TurnManager.Instance.CanEndTurn;

        if (moveButton != null) moveButton.interactable = canMove;
        if (endTurnButton != null) endTurnButton.interactable = canEnd;

        if (attackButton != null) attackButton.interactable = canAct;
        if (castSpellButton != null) castSpellButton.interactable = canAct;
        if (searchTreasureButton != null) searchTreasureButton.interactable = canAct;
        if (searchSecretDoorsButton != null) searchSecretDoorsButton.interactable = canAct;
        if (searchTrapsButton != null) searchTrapsButton.interactable = canAct;
        if (disarmTrapButton != null) disarmTrapButton.interactable = canAct;
    }
}