using UnityEngine;


// Passive bonus on the hero's totals. Read by Hero (computed on demand, nothing is pushed).
[CreateAssetMenu(menuName = "Cards/Artifact/Stat Bonus")]
public class StatBonusArtifactSO : ArtifactCardSO
{
    public int bodyPointsBonus;     // Ring of robustness
    public int mindPointsBonus;     // Talisman of knowledge
    public int defenseDiceBonus;    // Wizard's cloak
}
