using UnityEngine;

public class HeroUi : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StatsPanel statsPanel;
    [SerializeField] private PortraitPanel portraitPanel;

    [Header("Hero")]
    [SerializeField] private HeroData startHero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (startHero != null)
        {
            SetHero(startHero);
        }
    }

    public void SetHero(HeroData hero)
    {
        statsPanel.SetHero(hero);
        portraitPanel.SetHero(hero);
    }
}