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
            "Movement Squares : " + monster.movementCells + "\n" +
            "Attack Dice : " + monster.attackDices + "\n" +
            "Defend Dice : " + monster.defenseDices + "\n" +
            "Body Points : " + monster.healthPoints + "\n" +
            "Mind Points : " + monster.mindPoints;

        box.SetActive(true);
    }

    public void ShowHero(HeroData hero)
    {
        titleText.text = hero.name + " :";

        linesText.text =
            "Attack Dice : " + hero.attackDices + "\n" +
            "Movement : " + hero.movementDices + " red dice\n" +
            "Defend Dice : " + hero.defenseDices + "\n" +
            "Starting Weapon : " + hero.weapon.name + "\n" +
            "Body Points : " + hero.bodyPoints + "\n" +
            "Mind Points : " + hero.mindPoints;

        box.SetActive(true);
    }

    public void Hide()
    {
        box.SetActive(false);
    }
}