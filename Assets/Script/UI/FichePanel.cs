using UnityEngine;
using TMPro;

public class FichePanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject box;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text linesText;

    private void Awake()
    {
        box.SetActive(false);
    }

    public void ShowMonster(MonsterData monster)
    {
        titleText.text = monster.monsterName + " :";

        linesText.text =
            "Cases de déplacement : " + monster.movementCells + "\n" +
            "Dés d'attaque : " + monster.attackDices + "\n" +
            "Dés de défense : " + monster.defenseDices + "\n" +
            "Points de vie : " + monster.healthPoints + "\n" +
            "Points d'esprit : " + monster.mindPoints;

        box.SetActive(true);
    }

    public void ShowHero(HeroData hero)
    {
        titleText.text = hero.name + " :";

        linesText.text =
            "Dés d'attaque : " + hero.attackDices + "\n" +
            "Déplacement : " + hero.movementDices + " dés rouges\n" +
            "Dés de défense : " + hero.defenseDices + "\n" +
            "Arme de départ : " + hero.weapon.name + "\n" +
            "Points de vie : " + hero.bodyPoints + "\n" +
            "Points d'esprit : " + hero.mindPoints;

        box.SetActive(true);
    }

    public void Hide()
    {
        box.SetActive(false);
    }
}