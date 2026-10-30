using System.Collections;
using HeroQuest.Quests;
using UnityEngine;
using UnityEngine.InputSystem;

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

    //private void Update()
    //{
    //    if (Keyboard.current == null) return;

    //    if (Keyboard.current.vKey.wasPressedThisFrame)
    //    {
    //        QuestManager.Instance.ForceEnd(QuestResult.Victory);
    //    }

    //    if (Keyboard.current.lKey.wasPressedThisFrame)
    //    {
    //        QuestManager.Instance.ForceEnd(QuestResult.Defeat);
    //    }
    //}
}