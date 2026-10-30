using System.Collections;
using HeroQuest.Quests;
using UnityEngine;

// Demo bootstrap: starts the chosen quest once the scene is ready.
public class QuestBootstrap : MonoBehaviour
{
    [SerializeField] private QuestData quest;   // drag the quest 1 asset here

    private IEnumerator Start()
    {
        yield return null;   // wait one frame so every Start() has subscribed (UI, camera, TurnManager)
        if (quest == null || QuestManager.Instance == null)
        {
            Debug.LogError("[Bootstrap] Missing quest or QuestManager.");
            yield break;
        }
        QuestManager.Instance.StartQuest(quest);
    }
}