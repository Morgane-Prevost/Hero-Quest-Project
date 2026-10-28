using UnityEngine;
using TMPro;

public class StatsPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text lifeText;
    [SerializeField] private TMP_Text spiritText;

    public void SetHero(Hero hero)
    {
        nameText.text = "Stats : " + hero.name;
        lifeText.text = "Body points : " + hero.currentBodyPoints;
        spiritText.text = "Mind Points : " + hero.mindPoints;
    }
}