using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleButtonText;
    [SerializeField] private GameObject questBox;
    [SerializeField] private ListPanel listPanel;
    [SerializeField] private InventoryPanel inventoryPanel;

    private void Awake()
    {
        toggleButton.onClick.AddListener(OnToggle);

        questBox.SetActive(false);
        toggleButtonText.text = "+";
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
