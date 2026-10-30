using System.Collections;
using UnityEngine;
using HeroQuest.Quests;

public class QuestLauncher : MonoBehaviour
{
    [Tooltip("Only used when the game scene is played without going through the main menu")]
    [SerializeField] private QuestData testQuest;

    private IEnumerator Start()
    {
        yield return null;

        QuestData quest = GameSession.CurrentQuest;

        if (quest == null)
        {
            quest = testQuest;
        }

        if (quest == null)
        {
            Debug.LogError("No quest to start : choose one in the main menu or fill Test Quest.");
            yield break;
        }

        GameSession.CurrentQuest = quest;

        QuestManager.Instance.StartQuest(quest);
    }
}