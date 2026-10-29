using System.Collections.Generic;
using UnityEngine;

// Rust: a sword or a helmet used by the hero is destroyed for good. Artifacts are never affected (card text).
// The cards it can destroy are listed in the asset: no equipment name is written in the code.
[CreateAssetMenu(menuName = "Cards/Dread Spells/Rust Spell")]
public class RustSpellSO : DreadSpellSO
{
    public List<CardSO> destroyable = new List<CardSO>();   // the sword and helmet cards

    public override bool IsValidCast(DreadCast cast)
        => cast.hero is IRustTarget target
        && cast.item != null
        && !(cast.item is IArtifact)
        && destroyable.Contains(cast.item)
        && target.IsEquipped(cast.item);

    public override void Cast(DreadCast cast, IRandomPicker picker)
        => ((IRustTarget)cast.hero).DestroyEquipment(cast.item);
}