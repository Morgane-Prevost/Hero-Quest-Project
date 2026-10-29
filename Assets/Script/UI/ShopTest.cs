using UnityEngine;

public class ShopTest : MonoBehaviour
{
    [SerializeField] private Hero hero;
    [SerializeField] private ShopPanel shopPanel;
    [SerializeField] private int startGold = 500;

    private void Start()
    {
        hero.gold = startGold;
        shopPanel.SetBuyer(hero);
    }
}