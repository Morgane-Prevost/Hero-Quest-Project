using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Quests
{
    /// <summary>
    /// SEUL SCRIPT À ADAPTER : relie le système de quêtes à ton jeu existant.
    /// Remplace chaque TODO par l'appel à tes vrais scripts.
    /// </summary>
    public class HeroQuestWorldBridge : MonoBehaviour, IQuestWorld
    {
        // TODO: [SerializeField] GridManager grid; HeroManager heroes; MonsterManager monsters;

        // ── Mise en place ──
        public void LoadMap(TextAsset mapJson)            { /* grid.LoadFromJson(mapJson.text); */ }
        public void PlaceStairs(Vector2Int pos)           { /* grid.PlaceObject("stairs", pos); */ }

        public void PlaceHeroes(QuestData quest)
        {
            // OnStairs  -> heroes.PlaceAround(quest.stairsPosition)
            // InRoom    -> heroes.PlaceInRoom(quest.startRoomId)     (quête 6 : "Cellule")
            // Attention : ne pas lever HeroMoved ici.
        }

        public void ApplyNamedMonster(NamedMonsterDef d)
        {
            // var m = monsters.FindAt(d.tile) ?? monsters.FindById(d.instanceId);
            // m.SetStats(d.movement, d.attackDice, d.defenseDice, d.bodyPoints, d.mindPoints);
            // m.QuestId = d.instanceId;  m.DisplayName = d.displayName;
            // m.Spells = d.spells;       m.CastsInsteadOfAttacking = d.castsSpellInsteadOfAttacking;
        }

        public void SetWanderingMonster(string monsterId) { /* monsters.WanderingType = monsterId; (utilisé à la pioche d'une carte "Monstre errant") */ }

        // ── Monde ──
        public void SpawnMonster(string id, Vector2Int p) { /* monsters.Spawn(id, p); */ }
        public void SpawnObject(string id, Vector2Int p)  { /* grid.PlaceObject(id, p); */ }
        public void SetDoorOpen(string id, bool open)     { /* grid.GetDoor(id).SetOpen(open); */ }
        public void RevealRoom(string roomId)             { /* grid.RevealRoom(roomId); */ }

        // ── Héros ──
        public void LockHeroEquipment(int atkDice, int defDice)
        {
            // foreach (var h in heroes.All) { h.StashEquipment(); h.CanCastSpells = false;
            //                                  h.OverrideDice(atkDice, defDice); }
        }
        public void RestoreHeroEquipment(string heroId)   { /* var h = heroes.Get(heroId); h.RestoreEquipment(); h.CanCastSpells = true; h.ClearDiceOverride(); */ }
        public void GiveCard(string heroId, ScriptableObject card) { /* heroes.Get(heroId).Inventory.Add(card as TonTypeCarteSO); */ }
        public void GiveGold(string heroId, int amount)   { /* heroes.Get(heroId).Gold += amount; */ }
        public void RemoveHeroFromBoard(string heroId)    { /* heroes.Get(heroId).Escape(); */ }

        public IReadOnlyList<string> HeroIds              => new List<string>(); /* heroes.All.Select(h => h.Id).ToList() */
        public string HeroIdOfClass(string classId)       => null;               /* heroes.FindByClass(classId)?.Id */
        public int HeroesOnBoardCount                     => 0;                  /* heroes.All.Count(h => h.IsAlive && !h.HasEscaped) */
        public bool AllHeroesOnBoardOn(Vector2Int pos)    => false;              /* heroes.OnBoard.All(h => h.GridPos == pos) */
    }
}
