using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Quests
{
    [CreateAssetMenu(menuName = "HeroQuest/Actions/Show Note")]
    public class ShowNoteAction : QuestAction
    {
        public string noteLetter;        // "A", "B"... -> title "Note A"
        public string title = "Note";    // used when there is no letter
        [TextArea(3, 12)] public string text;
        public override void Execute(QuestContext c)
            => QuestUtil.Note(string.IsNullOrEmpty(noteLetter) ? title : "Note " + noteLetter, text);
    }

    /// <summary>Loot given by the quest: gold + CardSO cards + artifacts (by ArtifactInfo.id).</summary>
    [CreateAssetMenu(menuName = "HeroQuest/Actions/Grant Loot")]
    public class GrantLootAction : QuestAction
    {
        public RecipientType recipient = RecipientType.Actor;
        public HeroType heroType;                          // when recipient = SpecificHeroType
        public int gold;
        public List<CardSO> cards = new List<CardSO>();
        public List<string> artifactIds = new List<string>();
        public bool replacesTreasureDraw = true;           // no card drawn from the deck for this search

        public override void Execute(QuestContext c)
        {
            foreach (var id in c.ResolveRecipients(recipient, heroType))
            {
                if (gold > 0) c.World.GiveGold(id, gold);
                foreach (var card in cards) if (card != null) c.World.GiveCard(id, card);
                foreach (var art in artifactIds) if (!string.IsNullOrEmpty(art)) c.World.GrantArtifact(id, art);
            }
            if (replacesTreasureDraw) c.MarkTreasureHandled();
        }
    }

    /// <summary>The search is resolved by the quest with no loot (empty chest, useless rack, equipment found...).</summary>
    [CreateAssetMenu(menuName = "HeroQuest/Actions/Consume Treasure Search")]
    public class ConsumeTreasureSearchAction : QuestAction
    { public override void Execute(QuestContext c) => c.MarkTreasureHandled(); }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Restore Equipment")]
    public class RestoreEquipmentAction : QuestAction
    {
        public RecipientType recipient = RecipientType.Actor;
        public override void Execute(QuestContext c)
        { foreach (var id in c.ResolveRecipients(recipient, default)) c.World.RestoreHeroEquipment(id); }
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Reveal Zone")]
    public class RevealZoneAction : QuestAction
    {
        public int zone;
        public override void Execute(QuestContext c) => c.World.RevealZone(zone);
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Reveal Element")]
    public class RevealElementAction : QuestAction
    {
        public string elementId;      // secret door, hidden furniture, monster...
        public override void Execute(QuestContext c) => c.World.RevealElement(elementId);
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Open Door")]
    public class OpenDoorAction : QuestAction
    {
        public string doorId;
        public override void Execute(QuestContext c) => c.World.OpenDoor(doorId);
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Reveal Everything")]
    public class RevealEverythingAction : QuestAction
    { public override void Execute(QuestContext c) => c.World.RevealEverything(); }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Spawn Monster")]
    public class SpawnMonsterAction : QuestAction
    {
        public string monsterType; public Vector2Int cell;
        public override void Execute(QuestContext c) => c.World.SpawnMonster(monsterType, cell);
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Activate Monster")]
    public class ActivateMonsterAction : QuestAction
    {
        public string elementId;
        public override void Execute(QuestContext c) => c.World.ActivateMonster(elementId);
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Spawn Ally")]
    public class SpawnAllyAction : QuestAction
    {
        public string allyId;
        public bool controlledByActor = true;     // Sir Ragnar is moved by the hero who opened his cell
        public override void Execute(QuestContext c)
        {
            var def = c.Quest.allies.Find(a => a.id == allyId);
            if (def == null) { Debug.LogError("[Quest] Unknown ally '" + allyId + "'."); return; }
            c.World.SpawnAlly(def, controlledByActor ? c.CurrentActorId : "");
        }
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Damage Actor")]
    public class DamageActorAction : QuestAction
    {
        public int amount = 1;
        public override void Execute(QuestContext c)
        { if (!string.IsNullOrEmpty(c.CurrentActorId)) c.World.DamageHero(c.CurrentActorId, amount); }
    }

    /// <summary>Quest 13 rockfall: roll 1 red die, hurt on 'threshold'+ (helmet: only on 'helmetThreshold'+).</summary>
    [CreateAssetMenu(menuName = "HeroQuest/Actions/Roll Damage Actor")]
    public class RollDamageActorAction : QuestAction
    {
        public int damage = 1, threshold = 4, helmetThreshold = 6;
        public override void Execute(QuestContext c)
        {
            string id = c.CurrentActorId;
            if (string.IsNullOrEmpty(id)) return;
            int roll = c.World.RollRedDie();
            int needed = c.World.HeroWearsHelmet(id) ? helmetThreshold : threshold;
            if (roll >= needed) c.World.DamageHero(id, damage);
        }
    }

    /// <summary>Carrying a chest (quest 4): the hero rolls at most 'maxDice' movement dice. -1 removes the limit.</summary>
    [CreateAssetMenu(menuName = "HeroQuest/Actions/Limit Movement Dice")]
    public class LimitMovementDiceAction : QuestAction
    {
        public int maxDice = 1;
        public override void Execute(QuestContext c)
        { if (!string.IsNullOrEmpty(c.CurrentActorId)) c.World.LimitMovementDice(c.CurrentActorId, maxDice); }
    }

    /// <summary>
    /// Quest 10 teleporting doors: roll 'dice' red dice and teleport the hero to the matching cell.
    /// SIMPLIFIED rule: if the cell is occupied, roll again (the book makes the occupant lose 1 body point).
    /// </summary>
    [CreateAssetMenu(menuName = "HeroQuest/Actions/Teleport By Dice")]
    public class TeleportByDiceAction : QuestAction
    {
        [Serializable] public struct Entry { public int roll; public bool toStairs; public Vector2Int cell; }
        public int dice = 2;
        public List<Entry> table = new List<Entry>();

        public override void Execute(QuestContext c)
        {
            string hero = c.CurrentActorId;
            if (string.IsNullOrEmpty(hero)) return;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                int total = 0;
                for (int i = 0; i < dice; i++) total += c.World.RollRedDie();

                Entry? found = null;
                foreach (var e in table) if (e.roll == total) { found = e; break; }
                if (found == null) { Debug.LogWarning("[Quest] Teleport table has no entry for a roll of " + total); return; }

                if (found.Value.toStairs) { c.World.TeleportHeroToStairs(hero); return; }
                if (c.World.TeleportHero(hero, found.Value.cell)) return;
            }
        }
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Seal Passage")]
    public class SealPassageAction : QuestAction
    {
        public List<Vector2Int> cells = new List<Vector2Int>();
        public override void Execute(QuestContext c) => c.World.BlockCells(cells);
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Set Flag")]
    public class SetFlagAction : QuestAction
    {
        public string flag; public bool value = true;
        public override void Execute(QuestContext c) => c.SetFlag(flag, value);
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Complete Objective")]
    public class CompleteObjectiveAction : QuestAction
    {
        public string objectiveId;
        public override void Execute(QuestContext c) => c.CompleteObjective(objectiveId);
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/End Quest")]
    public class EndQuestAction : QuestAction
    {
        public QuestResult result = QuestResult.Defeat;
        public override void Execute(QuestContext c) => QuestManager.Instance.ForceEnd(result);
    }
}
