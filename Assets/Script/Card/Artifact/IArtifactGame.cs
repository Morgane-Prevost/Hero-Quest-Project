using System.Collections.Generic;

// What an artifact power may ask of the game. Implemented by the game layer(board, monsters, heroes)
public interface IArtifactGame
{
    IReadOnlyList<Hero> HeroesInSightOf(Hero hero);
    void TeleportToQuestStart(Hero hero);
    void SkipNextTurn(Monster monster);     // the monster stays imprisoned during Zargon's next turn
    void Revive(Hero deadHero);             // full body and mind points
}
