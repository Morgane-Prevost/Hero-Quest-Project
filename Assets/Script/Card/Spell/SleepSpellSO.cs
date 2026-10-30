using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Spells/Sleep Spell")]
public class SleepSpellSO :SpellCardSO
{
    public string[] immuneTags; // monster families the spell does not work

    public override bool CanTarget(ISpellMonsterTarget target)
    {
        foreach(string tag in immuneTags)
        {
            if (target.HasTag(tag)) return false;
        }
        return true;
    }
    public override void CastOnMonster(ISpellMonsterTarget target, IRandomPicker picker)
    {
        target.FallAsleep();
        target.TryBreakSleep(picker);
    }

    public override SpellTargets Targets => SpellTargets.Monster;
}
