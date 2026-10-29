using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform content;
    [SerializeField] private Button slotPrefab;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    [Header("Shop")]
    [SerializeField] private List<CardSO> shopCards;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color selectedColor = new Color(0.94f, 0.62f, 0.15f);

    private Hero buyer;
    private List<Button> slots = new List<Button>();
    private int selectedIndex = -1;

    private void Awake()
    {
        buyButton.onClick.AddListener(OnBuyClicked);
        messageText.text = "";

        BuildSlots();
    }

    public void SetBuyer(Hero newBuyer)
    {
        buyer = newBuyer;
        selectedIndex = -1;
        messageText.text = "";
        Refresh();
    }

    private void BuildSlots()
    {
        foreach (Button slot in slots)
        {
            Destroy(slot.gameObject);
        }
        slots.Clear();
        selectedIndex = -1;

        for (int i = 0; i < shopCards.Count; i++)
        {
            int index = i;

            Button slot = Instantiate(slotPrefab, content);
            slot.GetComponentInChildren<CardView>().Show(shopCards[i]);
            slot.onClick.AddListener(() => OnSlotClicked(index));
            slots.Add(slot);
        }

        Refresh();
    }

    private void OnSlotClicked(int index)
    {
        selectedIndex = index;
        messageText.text = GetBuyProblem(shopCards[index]);
        Refresh();
    }

    private string GetBuyProblem(CardSO card)
    {
        if (buyer == null)
        {
            return "No hero is shopping.";
        }

        if (card.goldCost <= 0)
        {
            return "This card is not for sale.";
        }

        if (buyer.gold < card.goldCost)
        {
            return "Not enough gold : " + (card.goldCost - buyer.gold) + " missing.";
        }

        return "";
    }

    private void OnBuyClicked()
    {
        if (selectedIndex < 0 || selectedIndex >= shopCards.Count)
            return;

        CardSO card = shopCards[selectedIndex];

        string problem = GetBuyProblem(card);
        if (problem != "")
        {
            messageText.text = problem;
            return;
        }

        buyer.gold -= card.goldCost;
        buyer.AddCardToInventory(card);

        shopCards.RemoveAt(selectedIndex);
        BuildSlots();

        messageText.text = "You bought : " + card.displayName;
    }

    private void Refresh()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i == selectedIndex)
            {
                slots[i].image.color = selectedColor;
            }
            else
            {
                slots[i].image.color = normalColor;
            }
        }

        if (buyer != null)
        {
            goldText.text = "Gold : " + buyer.gold;
        }

        if (selectedIndex < 0 || selectedIndex >= shopCards.Count)
        {
            buyButton.interactable = false;
            buyButtonText.text = "Buy";
            return;
        }

        CardSO card = shopCards[selectedIndex];
        buyButton.interactable = GetBuyProblem(card) == "";
        buyButtonText.text = "Buy (" + card.goldCost + ")";
    }
}