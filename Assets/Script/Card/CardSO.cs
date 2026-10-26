using UnityEngine;


// Common data shared by every card family (treasure, equipment, artifact, spells...)
// only put here what is true for ALL cards: no cost, no effect, no pile rule.
public abstract class CardSO : ScriptableObject
{
    public string displayName;
    [TextArea] public string description; // our own wording, not the official card text
    public Texture2D artwork;
}
