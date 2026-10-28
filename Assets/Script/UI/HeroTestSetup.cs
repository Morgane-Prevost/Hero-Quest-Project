using UnityEngine;

public class HeroTestSetup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Hero hero;
    [SerializeField] private InventoryPanel inventoryPanel;

    [Header("Test data")]
    [SerializeField] private HeroData heroData;
    [SerializeField] private CardSO[] inventoryCards;

    private void Start()
    {
        hero.heroName = heroData.name;
        hero.bodyPoints = heroData.bodyPoints;
        hero.mindPoints = heroData.mindPoints;
        hero.attackDices = heroData.attackDices;
        hero.defenseDices = heroData.defenseDices;
        hero.movementDices = heroData.movementDices;
        hero.weapon = heroData.weapon;
        hero.armors = new System.Collections.Generic.List<ArmorData>(heroData.armor);
        hero.gold = heroData.gold;

        foreach (CardSO card in inventoryCards)
        {
            hero.AddCardToInventory(card);
        }

        inventoryPanel.SetHero(hero);
    }
}