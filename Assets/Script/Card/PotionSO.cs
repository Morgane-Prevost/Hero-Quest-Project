using UnityEngine;

public abstract class PotionSO : ScriptableObject
{
    public string displayName;
    [TextArea] public string description; // our own wording, not the official card text
}
