// What a potion is allowed to ask of the hero who uses it.
// Implemented by Hero. Cards never depend on Hero directly.
public interface IPotionTarget
{
    // Restores body points, never above the hero's starting total
    void Heal(int amount);
    void AddAttackDiceNextAttack(int extraDice);
    void AddDefenseDiceNextDefense(int extraDice);
    void AttackTwiceNextAttack();
}