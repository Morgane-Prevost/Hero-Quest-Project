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
    [SerializeField] private QuestPanel questPanel;
    [SerializeField] private ListPanel listPanel;

    [Header("Slots")]
    [SerializeField] private Transform grid;
    [SerializeField] private Button slotPrefab;
    [SerializeField] private int slotCount = 12;

    [Header("Details")]
    [SerializeField] private RawImage detailIcon;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.1f, 0.11f, 0.13f);
    [SerializeField] private Color selectedColor = new Color(0.94f, 0.62f, 0.15f);

    private Hero hero;
    private List<Button> slots = new List<Button>();
    private List<CardSO> items = new List<CardSO>();
    private int selectedIndex = -1;

    private void Awake()
    {
        box.SetActive(false);

        pocketButton.onClick.AddListener(OnPocketClicked);
        closeButton.onClick.AddListener(Close);
        actionButton.onClick.AddListener(OnActionClicked);

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

        questPanel.Close();
        listPanel.ClearSelection();

        Refresh();
        box.SetActive(true);
    }

    public void Close()
    {
        box.SetActive(false);
    }

    private void OnSlotClicked(int index)
    {
        if (index >= items.Count)
            return;

        selectedIndex = index;
        Refresh();
    }

    private void Refresh()
    {
        items.Clear();

        if (hero  != null)
        {
            if (hero.weapon  != null)
            {
                items.Add(hero.weapon);
            }

            if (hero.armors  != null)
            {
                items.AddRange(hero.armors);
            }

            items.AddRange(hero.Inventory);

            titleText.text = "Inventory : " + hero.heroName;
            subtitleText.text =
                "Gold : " + hero.gold;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            RawImage icon = slots[i].GetComponentInChildren<RawImage>();

            if (i < items.Count)
            {
                icon.texture = items[i].icon;
                icon.enabled = items[i].icon != null;
            }
            else
            {
                icon.enabled = false;
            }

            if (i == selectedIndex)
            {
                slots[i].image.color = selectedColor;
            }
            else
            {
                slots[i].image.color = normalColor;
            }
        }

        ShowDetails();
    }

    private void ShowDetails()
    {
        if (selectedIndex < 0 || selectedIndex >= items.Count)
        {
            detailIcon.enabled = false;
            detailName.text = "";
            detailDescription.text = "";
            actionButton.gameObject.SetActive(false);
            return;
        }

        CardSO card = items[selectedIndex];

        detailIcon.texture = card.icon;
        detailIcon.enabled = card.icon != null;
        detailName.text = card.displayName;
        detailDescription.text = card.description;

        string label = GetActionLabel(card);
        actionButton.gameObject.SetActive(label != "");
        actionButtonText.text = label;
    }

    private string GetActionLabel(CardSO card)
    {
        if (card is WeaponData weaponCard)
        {
            if (hero.weapon == weaponCard)
            {
                return "Unequip";
            }
            return "Equip";
        }

        if (card is ArmorData armorCard)
        {
            if (hero.armors.Contains(armorCard))
            {
                return "Unequip";
            }
            return "Equip";
        }

        return "";
    }

    private void OnActionClicked()
    {
        if (selectedIndex < 0 || selectedIndex >= items.Count)
            return;

        CardSO card = items[selectedIndex];

        if (card is WeaponData weaponCard)
        {
            if (hero.weapon == weaponCard)
            {
                hero.unequipWeapon();
            }
            else
            {
                if (hero.weapon != null)
                {
                    hero.unequipWeapon();
                }
                hero.equipWeapon(weaponCard);
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

        Refresh();
        selectedIndex = items.IndexOf(card);
        Refresh();
    }
}
