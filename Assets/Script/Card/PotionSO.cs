using UnityEngine;

[CreateAssetMenu(menuName = "Potion")]
public class PotionSO : ScriptableObject
{
    public string displayName;
    [TextArea] public string description; // our own wording, not the official card text
}
