using UnityEngine;
using TMPro;

public class StatsPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text lifeText;
    [SerializeField] private TMP_Text spiritText;

    public void SetHero(HeroData hero)
    {
        nameText.text = "Stats : " + hero.name;
        lifeText.text = "Points de vie : " + hero.bodyPoints;
        spiritText.text = "Points d'esprit : " + hero.mindPoints;
    }
}