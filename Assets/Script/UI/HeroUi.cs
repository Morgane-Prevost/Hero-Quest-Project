using UnityEngine;

public class HeroUi : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StatsPanel statsPanel;
    [SerializeField] private PortraitPanel portraitPanel;
    [SerializeField] private CardViewer cardViewer;
    [SerializeField] private InventoryPanel inventoryPanel;
    [SerializeField] private ShopPanel shopPanel;

    private void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.HeroTurnStarted += OnHeroTurnStarted;

            if (TurnManager.Instance.CurrentHero != null)
            {
                SetHero(TurnManager.Instance.CurrentHero);
            }
        }
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.HeroTurnStarted -= OnHeroTurnStarted;
        }
    }

    private void OnHeroTurnStarted(Hero hero)
    {
        SetHero(hero);
    }

    public void SetHero(Hero hero)
    {
        if (hero == null) return;

        HeroData heroData = hero.Data;

        if (statsPanel != null) statsPanel.SetHero(hero);
        if (portraitPanel != null) portraitPanel.SetHero(heroData);
        if (cardViewer != null) cardViewer.SetHero(heroData);
        if (inventoryPanel != null) inventoryPanel.SetHero(hero);
        if (shopPanel != null) shopPanel.SetBuyer(hero);
    }
}