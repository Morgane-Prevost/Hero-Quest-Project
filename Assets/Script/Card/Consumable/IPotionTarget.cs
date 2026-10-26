// What a potion is allowed to ask of the hero who drinks it.
// Implemented by the Hero Manager side.
public interface IPotionTarget
{
    // Restores body points, never above the hero's starting total
    void Heal(int amount);
    void AddAttackDiceNextAttack(int extraDice);
    void AddDefenseDiceNextDefense(int extraDice);
    void AttackTwiceNextAttack();
}