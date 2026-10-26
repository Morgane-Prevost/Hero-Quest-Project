using System.Collections.Generic;

public class TreasureDeck
{
    private readonly List<TreasureCardSO> fullDeck;
    private readonly IRandomPicker picker;
    private readonly List<TreasureCardSO> pile = new List<TreasureCardSO>();
    private readonly List<TreasureCardSO> removed = new List<TreasureCardSO>();

    public int RemainingCount => pile.Count;

    public TreasureDeck(IEnumerable<TreasureCardSO> cards, IRandomPicker picker)
    {
        fullDeck = new List<TreasureCardSO>(cards);
        this.picker = picker;
        StartQuest();
    }

    // Start of a quest: all cards go back into the pile
    public void StartQuest()
    {
        pile.Clear();
        removed.Clear();
        pile.AddRange(fullDeck);
    }

    // Picking a uniform random index is equivalent to shuffling then taking the top card
    public TreasureCardSO Draw()
    {
        // case not covered by the rulebook
        // if(pile.Count == 0) return null;

        int index = picker.Pick(pile.Count);
        TreasureCardSO card = pile[index];

        if(!card.ReturnsToPile)
        {
            pile.RemoveAt(index);
            removed.Add(card);
        }
        return card;
    }
}
