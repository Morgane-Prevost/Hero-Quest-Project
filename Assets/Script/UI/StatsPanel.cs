using UnityEngine;
using TMPro;

public class StatsPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text lifeText;
    [SerializeField] private TMP_Text spiritText;

    [Header("Test values")]
    [SerializeField] private string testName = "Nain";
    [SerializeField] private int testLife = 7;
    [SerializeField] private int testSpirit = 3;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        SetStats(testName, testLife, testSpirit);
    }

    public void SetStats(string heroName, int life, int spirit)
    {
        nameText.text = "Stats : " + heroName;
        lifeText.text = "Points de vie : " + life;
        spiritText.text = "Points d'esprit : " + spirit;
    }
}
