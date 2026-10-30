using UnityEngine;

// Tornado: the targeted monster passes its next turn.
[CreateAssetMenu(menuName = "Cards/Spells/Skip Turn Spell")]
public class SkipTurnSpellSO : SpellCardSO
{
    public override void CastOnMonster(ISpellMonsterTarget target, IRandomPicker picker)
        => target.SkipNextTurn();

    public override SpellTargets Targets => SpellTargets.Monster;
}