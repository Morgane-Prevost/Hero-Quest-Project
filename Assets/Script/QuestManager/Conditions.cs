using UnityEngine;

namespace HeroQuest.Quests
{
    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Quest Started")]
    public class QuestStartedCondition : QuestCondition
    { public override bool Evaluate(QuestContext c, QuestEvent e) => e.Type == QuestEventType.QuestStarted; }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Room Entered")]
    public class RoomEnteredCondition : QuestCondition
    {
        public string roomId;
        public override bool Evaluate(QuestContext c, QuestEvent e)
            => e.Type == QuestEventType.RoomEntered && e.Id == roomId;
    }

    /// <summary>Un héros fouille les trésors d'une pièce (notes A/B des quêtes 3 et 6).</summary>
    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Treasure Searched")]
    public class TreasureSearchedCondition : QuestCondition
    {
        public string roomId;
        public override bool Evaluate(QuestContext c, QuestEvent e)
            => e.Type == QuestEventType.TreasureSearched && e.Id == roomId;
    }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Hero Escaped")]
    public class HeroEscapedCondition : QuestCondition
    { public override bool Evaluate(QuestContext c, QuestEvent e) => e.Type == QuestEventType.HeroEscaped; }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Monster Killed")]
    public class MonsterKilledCondition : QuestCondition
    {
        public string monsterId;     // instanceId d'un monstre nommé ("ulag") ou id de type
        public int count = 1;
        public override bool Evaluate(QuestContext c, QuestEvent e)
            => e.Type == QuestEventType.MonsterKilled && c.Kills(monsterId) >= count;
    }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Hero On Tile")]
    public class HeroOnTileCondition : QuestCondition
    {
        public Vector2Int tile;
        public override bool Evaluate(QuestContext c, QuestEvent e)
            => e.Type == QuestEventType.HeroMoved && e.Pos == tile;
    }

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Tile Searched (secret/piège)")]
    public class TileSearchedCondition : QuestCondition
    {
        public Vector2Int tile;
        public string searchKind;
        public override bool Evaluate(QuestContext c, QuestEvent e)
            => e.Type == QuestEventType.TileSearched && e.Pos == tile
               && (string.IsNullOrEmpty(searchKind) || e.Id == searchKind);
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

    [CreateAssetMenu(menuName = "HeroQuest/Conditions/Objective Done")]
    public class ObjectiveDoneCondition : QuestCondition
    {
        public string objectiveId;
        public override bool Evaluate(QuestContext c, QuestEvent e) => c.IsObjectiveDone(objectiveId);
    }
}
