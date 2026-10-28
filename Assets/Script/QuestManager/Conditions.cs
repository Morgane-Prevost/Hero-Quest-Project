using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Quests
{
    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Quest Started")]
    public class QuestStartedCondition : QuestCondition
    { public override bool Evaluate(QuestContext c, QuestEvent e) => e.Type == QuestEventType.QuestStarted; }

    /// <summary>A hero enters a room (zone number from the grid map).</summary>
    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Room Entered")]
    public class RoomEnteredCondition : QuestCondition
    {
        public int zone;
        public override bool Evaluate(QuestContext c, QuestEvent e)
            => e.Type == QuestEventType.RoomEntered && e.Zone == zone;
    }

    /// <summary>
    /// A hero searches for treasure. Either in room 'zone', or in the room containing the level element
    /// 'elementId' (the chest / rack the quest note refers to).
    /// </summary>
    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Treasure Searched")]
    public class TreasureSearchedCondition : QuestCondition
    {
        public int zone = -1;
        public string elementId;
        public override bool Evaluate(QuestContext c, QuestEvent e)
        {
            if (e.Type != QuestEventType.TreasureSearched) return false;
            if (zone >= 0 && e.Zone == zone) return true;
            return !string.IsNullOrEmpty(elementId) && c.World.IsElementInZone(elementId, e.Zone);
        }
    }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Hero Escaped")]
    public class HeroEscapedCondition : QuestCondition
    { public override bool Evaluate(QuestContext c, QuestEvent e) => e.Type == QuestEventType.HeroEscaped; }

    /// <summary>A monster dies: by level element id ("q3_ulag") and/or by type ("Orc"), count = total kills.</summary>
    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Monster Killed")]
    public class MonsterKilledCondition : QuestCondition
    {
        public string elementId;
        public string monsterType;
        public int count = 1;
        public override bool Evaluate(QuestContext c, QuestEvent e)
        {
            if (e.Type != QuestEventType.MonsterKilled) return false;
            if (!string.IsNullOrEmpty(elementId)) return c.KillsOfElement(elementId) >= count;
            if (!string.IsNullOrEmpty(monsterType)) return c.KillsOfType(monsterType) >= count;
            return false;
        }
    }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/All Monsters Dead")]
    public class AllMonstersDeadCondition : QuestCondition
    {
        public override bool Evaluate(QuestContext c, QuestEvent e)
            => e.Type == QuestEventType.MonsterKilled && c.World.RemainingLevelMonsters == 0;
    }

    /// <summary>A hero or ally (by id) moves and ends on the stairs (Sir Ragnar escorted).</summary>
    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Actor On Stairs")]
    public class ActorOnStairsCondition : QuestCondition
    {
        public string actorId;
        public override bool Evaluate(QuestContext c, QuestEvent e)
            => e.Type == QuestEventType.HeroMoved && e.ActorId == actorId && c.World.IsHeroOnStairs(actorId);
    }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Ally Died")]
    public class AllyDiedCondition : QuestCondition
    {
        public string allyId;
        public override bool Evaluate(QuestContext c, QuestEvent e)
            => e.Type == QuestEventType.HeroDied && e.Id == "ally" && e.ActorId == allyId;
    }

    /// <summary>A hero stops on one of these cells (rockfall cells of quest 13).</summary>
    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Hero On Tile")]
    public class HeroOnTileCondition : QuestCondition
    {
        public List<Vector2Int> tiles = new List<Vector2Int>();
        public override bool Evaluate(QuestContext c, QuestEvent e)
            => e.Type == QuestEventType.HeroMoved && tiles.Contains(e.Pos);
    }

    /// <summary>Every hero still on the board has walked on this cell (quest 12: "after the last hero passed").</summary>
    [CreateAssetMenu(menuName = "HeroQuest/Conditions/All Heroes Visited Tile")]
    public class AllHeroesVisitedTileCondition : QuestCondition
    {
        public Vector2Int tile;
        public override bool Evaluate(QuestContext c, QuestEvent e)
        {
            if (e.Type != QuestEventType.HeroMoved) return false;
            var ids = c.World.HeroIds;
            if (ids.Count == 0) return false;
            foreach (var id in ids) if (!c.HasVisited(id, tile)) return false;
            return true;
        }
    }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Item Picked Up")]
    public class ItemPickedUpCondition : QuestCondition
    {
        public string itemId;
        public override bool Evaluate(QuestContext c, QuestEvent e)
            => e.Type == QuestEventType.ItemPickedUp && e.Id == itemId;
    }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Door Opened")]
    public class DoorOpenedCondition : QuestCondition
    {
        public string doorId;
        public override bool Evaluate(QuestContext c, QuestEvent e)
            => e.Type == QuestEventType.DoorOpened && e.Id == doorId;
    }

    /// <summary>A hero walks through a door (quest 10 teleporting doors). Raised by the movement system.</summary>
    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Door Crossed")]
    public class DoorCrossedCondition : QuestCondition
    { public override bool Evaluate(QuestContext c, QuestEvent e) => e.Type == QuestEventType.DoorCrossed; }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Turn Reached")]
    public class TurnReachedCondition : QuestCondition
    {
        public int turn;
        public override bool Evaluate(QuestContext c, QuestEvent e)
            => e.Type == QuestEventType.TurnStarted && c.Turn >= turn;
    }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Flag")]
    public class FlagCondition : QuestCondition
    {
        public string flag;
        public bool expected = true;
        public override bool Evaluate(QuestContext c, QuestEvent e) => c.HasFlag(flag) == expected;
    }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/All Flags")]
    public class AllFlagsCondition : QuestCondition
    {
        public List<string> flags = new List<string>();
        public override bool Evaluate(QuestContext c, QuestEvent e)
        {
            if (flags.Count == 0) return false;
            foreach (var f in flags) if (!c.HasFlag(f)) return false;
            return true;
        }
    }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Objective Done")]
    public class ObjectiveDoneCondition : QuestCondition
    {
        public string objectiveId;
        public override bool Evaluate(QuestContext c, QuestEvent e) => c.IsObjectiveDone(objectiveId);
    }
}
