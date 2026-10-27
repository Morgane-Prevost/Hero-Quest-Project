using UnityEngine;

public abstract class ArtifactCardSO : CardSO, IArtifact
{
    [SerializeField] private ArtifactInfo info;
    public ArtifactInfo Info => info;
}
