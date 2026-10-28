using UnityEngine;

public class HeroUi : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StatsPanel statsPanel;
    [SerializeField] private PortraitPanel portraitPanel;
    [SerializeField] private CardViewer cardViewer;
    [SerializeField] private InventoryPanel inventoryPanel;

    private void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.HeroTurnStarted += OnHeroTurnStarted;
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
        statsPanel.SetHero(hero);
        portraitPanel.SetHero(hero.Data);
        cardViewer.SetHero(hero.Data);
        inventoryPanel.SetHero(hero);
    }
}