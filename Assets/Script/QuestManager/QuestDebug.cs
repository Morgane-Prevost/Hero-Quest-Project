using HeroQuest.Quests;
using UnityEngine;

public class QuestDebug : MonoBehaviour
{
    public QuestData quest;
    public string heroId = "barbarian";

    [ContextMenu("1 Start quest")]
    void StartIt() => QuestManager.Instance.StartQuest(quest);

    [ContextMenu("2 Log state")]
    void LogState()
    {
        var ctx = QuestManager.Instance.Context;
        Hero h = HeroManager.Instance.GetById(heroId);
        Debug.Log("[QuestDebug] running=" + QuestManager.Instance.IsRunning
            + " kill_verag=" + (ctx != null && ctx.IsObjectiveDone("kill_verag"))
            + " | " + heroId + " gold=" + h.gold + " hp=" + h.currentBodyPoints + " cell=" + h.Cell);
    }

    [ContextMenu("3 Fake search (zone 0 = fix me)")]
    void FakeSearch()
    {
        Hero h = HeroManager.Instance.GetById(heroId);
        bool handled = QuestManager.Instance.NotifyTreasureSearch(/*zone*/ 3, heroId, h.Cell);
        Debug.Log("[QuestDebug] treasure handled by quest: " + handled);
    }

    [ContextMenu("4 Kill Verag")]
    void KillVerag()
    {
        foreach (Monster m in MonsterManager.Instance.ActiveMonsters)
            if (m.elementId == "gargoyle_verag")
            {
                m.TakeDamage(999, HeroManager.Instance.GetById(heroId));
                return;
            }
        Debug.LogWarning("[QuestDebug] Verag not found (not spawned? wrong type?)");
    }
}
