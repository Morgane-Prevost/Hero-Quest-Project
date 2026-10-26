using UnityEngine;

public enum TreasureSearchOutcome
{
    NotInRoom,                  // corridor: nothing to search
    MonstersInRoom,             // forbidden while a monster is in the room
    AlreadySearched,            // this hero already searched this main
    FurnitureTrapTriggered,     // hero did not search for traps first (turn ends)
    SpecialTreasure,            // quest treasure found, no card drawn
    CardDawn,                   // normal draw from the treasure deck
}

public class TreasureManager
{
    private readonly TreasureDeck deck;
    private readonly ITreasureContext context;

    public void StartQuest()
    {
        deck.StartQuest();
    }

}
