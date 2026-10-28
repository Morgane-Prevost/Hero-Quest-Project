using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu(fileName = "ArtifactArmorData", menuName = "GameDatas/Equipement/ArtifactArmorData")]
public class ArtifactArmorData : ArmorData, IArtifact
{
    [SerializeField] private ArtifactInfo info;
    public ArtifactInfo Info => info;
}
