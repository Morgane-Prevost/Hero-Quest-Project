using System;
using UnityEngine;

// The caster reappears on the cell given by the quest map (known only by Zargon).
[CreateAssetMenu(menuName = "Cards/Dread Spells/Teleport Spell")]
public class TeleportSpellSO : DreadSpellSO
{
    public override bool IsValidCast(DreadCast cast) => cast.world != null && cast.caster != null && cast.world.IsFree(cast.cell);

    public override void Cast(DreadCast cast, IRandomPicker picker)
    {
        if (!cast.world.Teleport(cast.caster, cast.cell))
            throw new InvalidOperationException($"{displayName}: cannot teleport to {cast.cell}");
    }
}