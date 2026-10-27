using System;
using System.Collections.Generic;

// Tracks artifact usage. Pure C# so it can be tested without a scene.
public class ArtifactLedger
{
    private readonly Dictionary<string, CardSO> byId = new Dictionary<string, CardSO>(); // lookup by id
    private readonly HashSet<string> usedThisQuest = new HashSet<string>();
    private readonly HashSet<string> consumed = new HashSet<string>();      //single use, kept across quests
    private readonly List<string> lostIds = new List<string>();

    public ArtifactLedger(IEnumerable<CardSO> cards)
    {
        foreach (CardSO card in cards)
        {
            if (card == null) throw new ArgumentException("Empty slot in the artifact list");
            if (!(card is IArtifact artifact)) throw new ArgumentException($"{card.name} is not an artifact");
            string id = artifact.Info.id;
            if (string.IsNullOrEmpty(id)) throw new ArgumentException($"{card.name} has no id");
            if (byId.ContainsKey(id)) throw new ArgumentException($"Duplicate artifact id: {id}");
            byId.Add(id, card);
        }
    }

    public CardSO Get(string id) => byId.TryGetValue(id, out CardSO card) ? card : null;

    public bool IsAvailable(IArtifact artifact)
    {
        string id = artifact.Info.id;
        switch (artifact.Info.usage)
        {
            case ArtifactUsage.OncePerQuest: return !usedThisQuest.Contains(id);
            case ArtifactUsage.SingleUse: return !consumed.Contains(id);
            default: return true;
        }
    }

    // Marks the power as used. Returns false if it was not available.
    public bool TryUse(IArtifact artifact)
    {
        if (!IsAvailable(artifact)) return false;
        string id = artifact.Info.id;
        if(artifact.Info.usage == ArtifactUsage.OncePerQuest) usedThisQuest.Add(id);
        else if (artifact.Info.usage == ArtifactUsage.SingleUse) consumed.Add(id);
        return true;
    }

    // Once-per-quest powers come back
    public void StartQuest() => usedThisQuest.Clear();

    // A monster stole it from a dead hero: it goes in the first treasure of the next quest
    public void MarkLost(string id)
    {
        if(!lostIds.Contains(id)) lostIds.Add(id);
    }

    public List<string> TakeLost()
    {
        var result = new List<string>(lostIds);
        lostIds.Clear();
        return result;
    }
}
