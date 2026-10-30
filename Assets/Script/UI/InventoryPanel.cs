using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class InventoryPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject box;
    [SerializeField] private Button pocketButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    //[SerializeField] private QuestPanel questPanel;
    [SerializeField] private ListPanel listPanel;
    [SerializeField] private ShopPanel shopPanel;

    [Header("Slots")]
    [SerializeField] private Transform grid;
    [SerializeField] private Button slotPrefab;
    [SerializeField] private int slotCount = 12;

    [Header("Details")]
    [SerializeField] private CardView detailCard;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.1f, 0.11f, 0.13f);
    [SerializeField] private Color selectedColor = new Color(0.94f, 0.62f, 0.15f);

    private Hero hero;
    private List<Button> slots = new List<Button>();
    private List<CardSO> items = new List<CardSO>();
    private int selectedIndex = -1;

    private class SimpleRandomPicker : IRandomPicker
    {
        public int Pick(int maxExclusive) => Random.Range(0, maxExclusive);
    }

    private void Awake()
    {
        if (box != null) box.SetActive(false);

        if (pocketButton != null) pocketButton.onClick.AddListener(OnPocketClicked);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (actionButton != null) actionButton.onClick.AddListener(OnActionClicked);

        for (int i = 0; i < slotCount; i++)
        {
            int index = i;

            Button slot = Instantiate(slotPrefab, grid);
            slot.onClick.AddListener(() => OnSlotClicked(index));
            slots.Add(slot);
        }
    }

    public void SetHero(Hero newHero)
    {
        hero = newHero;
        selectedIndex = -1;
        Refresh();
    }

    private void OnPocketClicked()
    {
        if (box.activeSelf)
        {
            Close();
            return;
        }

        //if (questPanel != null) questPanel.Close();
        if (listPanel != null) listPanel.ClearSelection();
        if (shopPanel != null) shopPanel.Close();

        Refresh();
        box.SetActive(true);
    }

    public void Close()
    {
        if (box != null) box.SetActive(false);
    }

    private void OnSlotClicked(int index)
    {
        if (index >= items.Count) return;

        selectedIndex = index;
        Refresh();
    }

    public void Refresh()
    {
        items.Clear();

        if (hero != null)
        {
            if (hero.weapon != null) items.Add(hero.weapon);
            if (hero.armors != null) items.AddRange(hero.armors);
            items.AddRange(hero.Inventory);

            if (titleText != null) titleText.text = "Inventory : " + hero.heroName;
            if (subtitleText != null) subtitleText.text = "Gold : " + hero.gold;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            RawImage icon = slots[i].GetComponentInChildren<RawImage>();

            if (i < items.Count)
            {
                if (icon != null)
                {
                    icon.texture = items[i].icon;
                    icon.enabled = items[i].icon != null;
                }
            }
            else
            {
                if (icon != null) icon.enabled = false;
            }

            if (slots[i].image != null)
            {
                slots[i].image.color = (i == selectedIndex) ? selectedColor : normalColor;
            }
        }

        ShowDetails();
    }

    private void ShowDetails()
    {
        if (selectedIndex < 0 || selectedIndex >= items.Count)
        {
            if (detailCard != null) detailCard.gameObject.SetActive(false);
            if (actionButton != null) actionButton.gameObject.SetActive(false);
            return;
        }

        CardSO card = items[selectedIndex];

        if (detailCard != null)
        {
            detailCard.gameObject.SetActive(true);
            detailCard.Show(card);
        }

        string label = GetActionLabel(card);
        if (actionButton != null)
        {
            actionButton.gameObject.SetActive(label != "");
            if (actionButtonText != null) actionButtonText.text = label;
        }
    }

    private string GetActionLabel(CardSO card)
    {
        if (card is WeaponData weaponCard)
        {
            if (hero.weapon == weaponCard) return "";
            return "Equip";
        }

        if (card is ArmorData armorCard)
        {
            return hero.armors.Contains(armorCard) ? "Unequip" : "Equip";
        }

        if (card is PotionCardSO)
        {
            return "Use";
        }

        return "";
    }

    private void OnActionClicked()
    {
        if (selectedIndex < 0 || selectedIndex >= items.Count) return;

        CardSO card = items[selectedIndex];

        if (card is WeaponData weaponCard)
        {
            WeaponData oldWeapon = hero.weapon;

            hero.unequipWeapon();

            if (hero.canEquipWeapon(weaponCard))
            {
                hero.equipWeapon(weaponCard);
            }
            else
            {
                hero.equipWeapon(oldWeapon);
            }
        }
        else if (card is ArmorData armorCard)
        {
            if (hero.armors.Contains(armorCard))
            {
                hero.unEquipArmor(armorCard);
            }
            else
            {
                hero.EquipArmor(armorCard);
            }
        }
        else if (card is PotionCardSO potionCard)
        {
            bool used = hero.UsePotion(potionCard, new SimpleRandomPicker());
            if (used)
            {
                selectedIndex = -1;
            }
        }

        Refresh();
    }
}