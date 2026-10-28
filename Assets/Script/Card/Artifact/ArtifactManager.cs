using System.Collections.Generic;
using UnityEngine;

public class ArtifactManager : MonoBehaviour
{
    [SerializeField] private List<CardSO> artifacts;

    private ArtifactLedger ledger;

    private void Awake()
    {
        ledger = new ArtifactLedger(artifacts);
    }

    // Call it at the same place as TreasureManager.StartQuest
    public void StartQuest() => ledger.StartQuest();

    // A quest note reveals an artifact: it goes to the hero who found it
    public bool Grant(string id, Hero hero)
    {
        if (hero == null)
        {
            Debug.LogError("Grant: no hero given");
            return false;
        }
        CardSO card = ledger.Get(id);
        if(card== null)
        {
            Debug.LogError($"Unknown artifact id: '{id}'");
            return false;
        }
        hero.AddCardToInventory(card);
        return true;
    }
    public bool IsAvailable(IArtifact artifact) => ledger.IsAvailable(artifact);
    public bool TryUse(IArtifact artifact) => ledger.TryUse(artifact);
    public void MarkLost(string id) => ledger.MarkLost(id);
    public List<string> TakeLostArtifacts() => ledger.TakeLost();
}
