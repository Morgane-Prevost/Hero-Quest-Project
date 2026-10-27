using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Artifact/Life Elixir")]
public class LifeElixirSO : ArtifactCardSO
{
    public void Activate(Hero deadHero, IArtifactGame game) => game.Revive(deadHero);

}
