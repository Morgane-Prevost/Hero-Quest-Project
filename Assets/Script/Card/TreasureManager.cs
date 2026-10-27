using UnityEngine;

// Owns the treasure deck for the current game. Other systems ask it for a card.
public class TreasureManager : MonoBehaviour
{
    [SerializeField] private TreasureDeckSO deckData;

    private TreasureDeck deck;

    public int RemainingCount => deck.RemainingCount;

    private void Awake()
    {
        // Default picker; the game can replace it with Initialize(replays, tests)
        if (deck==null) Initialize(new SystemRandomPicker());
    }

    // The game passes the ONE shared picker here (same one as the dice)
    public void Initialize(IRandomPicker picker)
    {
        deck = new TreasureDeck(deckData.cards, picker);
    }
    // Start of a quest: every card goes back into the pile
    public void StartQuest() => deck.StartQuest();

    // Only picks the card: the caller shows it, then calls card.Resolve(context)
    public TreasureCardSO Draw() => deck.Draw();

}
