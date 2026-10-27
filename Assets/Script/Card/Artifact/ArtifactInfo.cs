using UnityEngine;
using System;

// How often an artifact power can be used
public enum ArtifactUsage
{
    Unlimited = 0,      // passive items
    OncePerQuest = 1,   // "one time per quest": comes back at the start of each quest
    SingleUse =2,       // "Can be used just one time" : consumed
}

// Data shared by every artifact, whatever its base class (CardSO, WeaponData, ArmorData)
[Serializable]
public class ArtifactInfo
{
    public string id;                       // stable key used by quest data
    public ArtifactUsage usage;
    public HeroType[] onlyHeroes;           // empty = no restriction
    public HeroType[] forbiddenHeroes;      // empty = no restriction

    public bool CanBeUsedBy(HeroType type)
    {
        if (onlyHeroes != null && onlyHeroes.Length > 0 && Array.IndexOf(onlyHeroes, type) < 0) return false; 
        if(forbiddenHeroes != null && Array.IndexOf(forbiddenHeroes, type) >= 0) return false;
        return true;
    }
}

public interface IArtifact
{
    ArtifactInfo Info { get; }
}
