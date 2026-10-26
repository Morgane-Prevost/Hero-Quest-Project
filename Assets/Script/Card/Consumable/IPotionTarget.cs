// What a potion is allowed to ask of the hero who drinks it.
// Implemented by the Hero Manager side.
public interface IPotionTarget
{
    // The hero rolls N red dice, capped at the hero's starting life points
    void Heal(int amount);
    void AddAttackDiceNextAttack(int extraDice);
    void AddDefenseDiceNextDefense(int extraDice);
    void AttackTwiceNextAttack();
}