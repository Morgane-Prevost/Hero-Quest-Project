using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Quests
{
    /// <summary>Tout ce dont le système de quêtes a besoin du jeu existant (implémenté par HeroQuestWorldBridge).</summary>
    public interface IQuestWorld
    {
        // Mise en place
        void LoadMap(TextAsset mapJson);
        void PlaceStairs(Vector2Int pos);
        void PlaceHeroes(QuestData quest);                    // sur l'escalier OU dans quest.startRoomId
        void ApplyNamedMonster(NamedMonsterDef def);          // Ulag, Grak : stats / sorts spécifiques
        void SetWanderingMonster(string monsterId);

        // Monde
        void SpawnMonster(string monsterId, Vector2Int pos);
        void SpawnObject(string objectId, Vector2Int pos);
        void SetDoorOpen(string doorId, bool open);
        void RevealRoom(string roomId);

        // Héros
        void LockHeroEquipment(int attackDice, int defenseDice);   // équipement confisqué + sorts bloqués
        void RestoreHeroEquipment(string heroId);
        void GiveCard(string heroId, ScriptableObject card);
        void GiveGold(string heroId, int amount);
        void RemoveHeroFromBoard(string heroId);                   // s'échappe par l'escalier

        IReadOnlyList<string> HeroIds { get; }                     // tous les héros de la partie
        string HeroIdOfClass(string classId);                      // "wizard", "barbarian"... ou null
        int HeroesOnBoardCount { get; }                            // vivants ET pas encore échappés
        bool AllHeroesOnBoardOn(Vector2Int pos);
    }
}
