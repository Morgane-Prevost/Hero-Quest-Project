using System.Collections.Generic;
using UnityEngine;
using HeroQuest.Quests;

[CreateAssetMenu(fileName = "QuestCatalog", menuName = "GameDatas/QuestCatalog")]
public class QuestCatalog : ScriptableObject
{
    public List<QuestData> quests;

    public QuestData GetNext(QuestData current)
    {
        int index = quests.IndexOf(current);
        if (index < 0 || index + 1 >= quests.Count) return null;
        return quests[index + 1];
    }
}