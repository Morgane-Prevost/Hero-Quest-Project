using UnityEngine;


public abstract class TreasureCardSO : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public Sprite artwork;

    public virtual bool ReturnsToPile => false;

    public abstract void Resolve(ITreasureContext context);
}
