using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Quests
{
    /// <summary>Everything the quest system needs from the game. Implemented by HeroQuestWorldBridge.</summary>
    public interface IQuestWorld
    {
        // ── Setup ──
        void BeginQuest();                                   // treasure deck + artifact ledger reset, allies removed
        void LoadLevel(TextAsset levelJson);                 // LevelBuilder.Build() on the quest's LevelData JSON
        void PlaceHeroes(QuestData quest);                   // on the stairs, or in quest.startZone
        void SetWanderingMonster(string monsterType, string note);

        // ── Level ──
        void RevealZone(int zone);
        void RevealElement(string elementId);
        void OpenDoor(string doorId);
        void RevealEverything();                             // all doors open, everything shown (quest 2)
        void SpawnMonster(string monsterType, Vector2Int cell);
        void ActivateMonster(string elementId);              // statue wakes up
        void SpawnAlly(AllyDef def, string controllerHeroId);
        void BlockCells(IList<Vector2Int> cells);            // rockfall / sealed passage
        bool IsElementInZone(string elementId, int zone);
        int RemainingLevelMonsters { get; }                  // level monsters not killed yet (hidden ones included)

        // ── Heroes ──
        IReadOnlyList<string> HeroIds { get; }               // living heroes (allies excluded, escaped included)
        string HeroIdOfType(HeroType type);
        bool IsAlly(string id);
        int HeroesOnBoardCount { get; }                      // alive AND not escaped
        bool IsHeroOnStairs(string heroId);
        bool AllHeroesOnBoardOnStairs();
        void LockHeroEquipment(int attackDice, int defenseDice);
        void RestoreHeroEquipment(string heroId);
        void GiveCard(string heroId, CardSO card);
        void GrantArtifact(string heroId, string artifactId);
        void GiveGold(string heroId, int amount);
        void DamageHero(string heroId, int amount);
        void LimitMovementDice(string heroId, int maxDice);  // -1 = no limit
        void RemoveHeroFromBoard(string heroId);             // escaped through the stairs
        bool TeleportHero(string heroId, Vector2Int cell);   // false if the cell is occupied
        void TeleportHeroToStairs(string heroId);
        bool HeroWearsHelmet(string heroId);
        int RollRedDie();
    }
}
