using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

public class ShopPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject box;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform content;
    [SerializeField] private Button slotPrefab;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    [Header("Connected UI")]
    [SerializeField] private InventoryPanel inventoryPanel;
    [SerializeField] private GameObject gameUI;

    [Header("Shop")]
    [FormerlySerializedAs("shopCards")]
    [SerializeField] private List<CardSO> allCards;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color selectedColor = new Color(0.94f, 0.62f, 0.15f);

    private Hero buyer;
    private List<Button> slots = new List<Button>();
    private List<CardSO> shopCards = new List<CardSO>();
    private int selectedIndex = -1;

    private void Awake()
    {
        if (box != null) box.SetActive(false);

        if (shopButton != null) shopButton.onClick.AddListener(ToggleShop);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
        if (messageText != null) messageText.text = "";

        FillShop();
        BuildSlots();
    }

    private void FillShop()
    {
        shopCards = new List<CardSO>(allCards);
    }

    public void ToggleShop()
    {
        if (box != null && box.activeSelf)
        {
            Close();
            return;
        }

        if (inventoryPanel != null) inventoryPanel.Close();

        Refresh();
        if (box != null) box.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);
    }

    public void Close()
    {
        if (box != null) box.SetActive(false);
        if (gameUI != null) gameUI.SetActive(true);
    }

    public void SetBuyer(Hero newBuyer)
    {
        buyer = newBuyer;
        selectedIndex = -1;
        if (messageText != null) messageText.text = "";
        Refresh();
    }

    private void BuildSlots()
    {
        foreach (Button slot in slots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        slots.Clear();
        selectedIndex = -1;

        for (int i = 0; i < shopCards.Count; i++)
        {
            int index = i;

            Button slot = Instantiate(slotPrefab, content);
            CardView cardView = slot.GetComponentInChildren<CardView>();
            if (cardView != null) cardView.Show(shopCards[i]);

            slot.onClick.AddListener(() => OnSlotClicked(index));
            slots.Add(slot);
        }

        Refresh();
    }

    private void OnSlotClicked(int index)
    {
        selectedIndex = index;
        if (messageText != null) messageText.text = GetBuyProblem(shopCards[index]);
        Refresh();
    }

    private string GetBuyProblem(CardSO card)
    {
        if (buyer == null) return "No hero is shopping.";
        if (card.goldCost <= 0) return "This card is not for sale.";
        if (buyer.gold < card.goldCost) return "Not enough gold : " + (card.goldCost - buyer.gold) + " missing.";
        return "";
    }

    private void OnBuyClicked()
    {
        if (selectedIndex < 0 || selectedIndex >= shopCards.Count) return;

        CardSO card = shopCards[selectedIndex];

        string problem = GetBuyProblem(card);
        if (problem != "")
        {
            if (messageText != null) messageText.text = problem;
            return;
        }

        buyer.gold -= card.goldCost;
        buyer.AddCardToInventory(card);

        shopCards.RemoveAt(selectedIndex);
        BuildSlots();

        if (inventoryPanel != null) inventoryPanel.Refresh();

        if (messageText != null) messageText.text = "You bought : " + card.displayName;
    }

    private void Refresh()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].image != null)
            {
                slots[i].image.color = (i == selectedIndex) ? selectedColor : normalColor;
            }
        }

        if (buyer != null && goldText != null)
        {
            goldText.text = "Gold : " + buyer.gold;
        }

        if (buyButton == null) return;

        if (selectedIndex < 0 || selectedIndex >= shopCards.Count)
        {
            buyButton.interactable = false;
            if (buyButtonText != null) buyButtonText.text = "Buy";
            return;
        }

        CardSO card = shopCards[selectedIndex];
        buyButton.interactable = GetBuyProblem(card) == "";
        if (buyButtonText != null) buyButtonText.text = "Buy (" + card.goldCost + ")";
    }
}