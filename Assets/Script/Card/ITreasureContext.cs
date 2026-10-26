public interface ITreasureContext
{
    void AddGold(int amount);
    void AddPotion(PotionSO potion);
    void LoseLife(int amount);
    void EndHeroTurn();
    void SpawnWanderingMonster();
}
