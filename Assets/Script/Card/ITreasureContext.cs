public interface ITreasureContext
{
    void AddGold(int amount);
    void AddPotion(PotionCardSO potion);
    void LoseLife(int amount);
    void EndHeroTurn();
    void SpawnWanderingMonster();
}
