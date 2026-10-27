using UnityEngine;

// Data read by the spell system (not implemented yet)
[CreateAssetMenu(menuName = "Cards/Artifact/Spell Casting")]
public class SpellCastingArtifactSO :ArtifactCardSO 
{
    public int spellsPerTurn = 1;           // Magic wand: 2 different spells
    public int castsOfAnnounceSpell = 1;    // Ring of incantation: 2, spell announced at quest start
}
