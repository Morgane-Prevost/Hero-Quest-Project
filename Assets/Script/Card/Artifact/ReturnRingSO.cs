using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Artifact/Return Ring")]
public class ReturnRingSO : ArtifactCardSO
{
    public void Activate(Hero wearer, IArtifactGame game)
    {
        foreach(Hero hero in game.HeroesInSightOf(wearer))
        {
            game.TeleportToQuestStart(hero);
        }
    }
}
