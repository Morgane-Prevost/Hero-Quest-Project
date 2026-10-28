using System.Collections.Generic;
using UnityEngine;

// Anything that can lose body points (heroes and monsters).
public interface IDamageable
{
    void TakeDamage(int amount);
}

// What a Terror spell is allowed to ask of a hero. Implemented by Hero.
public interface IDreadHeroTarget : IDamageable
{
    void AddEffect(HeroEffect effect, int value);
    bool TryBreakEffect(HeroEffect effect, IRandomPicker picker);   // true if the hero is free (or was not affected)
}

// Rust: destroys a piece of equipment the hero is using. Implemented by Hero.
public interface IRustTarget
{
    bool IsEquipped(CardSO item);
    bool DestroyEquipment(CardSO item);
}

// What a Terror spell is allowed to ask of the board. Implemented by BoardDreadWorld.
// Cell lists come in a fixed order (rooms: x then y). The picker is consumed in that order, so replays match.
public interface IDreadWorld
{
    bool IsInRoom(Vector2Int cell);                                            // false in a corridor
    IEnumerable<Vector2Int> AreaOf(Vector2Int cell);                           // the room or corridor that contains the cell
    IEnumerable<Vector2Int> LineFrom(Vector2Int start, Vector2Int direction);  // start excluded, stops at a wall or closed door
    IEnumerable<Vector2Int> FreeCellsAround(Vector2Int cell);                  // same room/corridor, nearest first
    bool IsFree(Vector2Int cell);
    IDamageable VictimAt(Vector2Int cell);                                     // hero or monster standing there, else null
    IDreadHeroTarget HeroAt(Vector2Int cell);
    Monster Spawn(MonsterData data, Vector2Int cell);                          // null if the cell is not free
    bool Teleport(Monster monster, Vector2Int cell);
}

// Everything a Terror spell may need. The caller fills only what the spell asks for.
public class DreadCast
{
    public Monster caster;          // filled by DreadSpellManager
    public IDreadWorld world;
    public IDreadHeroTarget hero;   // Fireball, Tornado, Sleep, Horror, Control, Rust
    public Vector2Int cell;         // Cloud: a cell of the room or corridor / Teleport: destination (from the quest)
    public Vector2Int direction;    // Lightning: (1,0), (1,1), (0,-1)...
    public CardSO item;             // Rust: the sword or helmet chosen by Zargon
}

// Base of Zargon's spell cards. One subclass per KIND of effect, values live in the assets.
// Stateless: casts left are tracked per monster by DreadSpellManager.
// No turn check and no line of sight here: the caller does it (rulebook p.21).
public class DreadSpellSO : CardSO
{
    // Spells that target one hero override CastOnHero. Spells that need the board override Cast.
    public virtual void CastOnHero(IDreadHeroTarget target, IRandomPicker picker)
        => throw new System.InvalidOperationException($"{displayName} cannot target a hero");

    // False = invalid target: nothing happens and the spell is not spent.
    public virtual bool IsValidCast(DreadCast cast) => cast.hero != null;

    public virtual void Cast(DreadCast cast, IRandomPicker picker) => CastOnHero(cast.hero, picker);
}