using UnityEngine;

[CreateAssetMenu(fileName = "LifeElexirSO", menuName = "Scriptable Objects/LifeElexirSO")]
public class LifeElexirSO : ArtifactCardSO
{
    public void Activate(Hero deadHero, IArtifactGame game) => game.Revive(deadHero);

}
