using UnityEngine;

// Base for potions found in treasure. One subclass per effect.
// Stateless: the asset can sit in several inventories, so state lives on the target.
// No turn check: using a potion is not an action (rulebook p.12, p.16, p.21).
public abstract class PotionSO : CardSO {
    public abstract void Use(IPotionTarget target, IRandomPicker picker);
}
