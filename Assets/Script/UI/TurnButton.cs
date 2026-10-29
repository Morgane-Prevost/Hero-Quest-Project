using UnityEngine;
using UnityEngine.UI;

public class TurnButtons : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button endTurnButton;

    private void Awake()
    {
        endTurnButton.onClick.AddListener(OnEndTurnClicked);
    }

    private void OnEndTurnClicked()
    {
        TurnManager.Instance.EndHeroTurn();
    }

    private void Update()
    {
        if (TurnManager.Instance == null)
            return;

        endTurnButton.interactable = TurnManager.Instance.CanEndTurn;
    }
}