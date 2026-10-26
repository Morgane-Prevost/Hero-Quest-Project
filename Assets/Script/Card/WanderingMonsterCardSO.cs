using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Cards/Treasure/Wandering Monster")]
public class WanderingMonsterCardSO : TreasureCardSO
{
    // Wandering monster cards go back into the pile
    public override bool ReturnsToPile => true;

    // The context decides which monster to spawn (defined by the quest)
    public override void Resolve(ITreasureContext context) => context.SpawnWanderingMonster();
}
