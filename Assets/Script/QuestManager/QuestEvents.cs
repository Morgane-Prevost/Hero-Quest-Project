using System;
using UnityEngine;

namespace HeroQuest.Quests
{
    public enum QuestEventType
    {
        QuestStarted, RoomEntered, MonsterKilled, HeroMoved, HeroDied, HeroEscaped,
        ItemPickedUp, DoorOpened, DoorCrossed, TileSearched, TreasureSearched,
        TrapDisarmed, TurnStarted, StateChanged
    }

    /// <summary>
    /// Id      = doorId / elementId (JSON) / itemId / flag, depending on the type.
    /// Kind    = MonsterKilled: monster TYPE ("Orc"), used for bounties.
    /// Zone    = room number (BoardModel zone), -1 = not applicable.
    /// ActorId = hero (or ally) who caused the event ("barbarian", "elf", "dwarf", "wizard"...).
    /// </summary>
    public readonly struct QuestEvent
    {
        public readonly QuestEventType Type;
        public readonly string Id;
        public readonly string Kind;
        public readonly Vector2Int Pos;
        public readonly int Zone;
        public readonly string ActorId;

        public QuestEvent(QuestEventType type, string id = "", Vector2Int pos = default,
                          string actorId = "", int zone = -1, string kind = "")
        {
            Type = type; Id = id ?? ""; Pos = pos; ActorId = actorId ?? ""; Zone = zone; Kind = kind ?? "";
        }
    }

    public static class QuestEvents
    {
        public static event Action<QuestEvent> OnEvent;

        public static void Raise(QuestEventType type, string id = "", Vector2Int pos = default,
                                 string actorId = "", int zone = -1, string kind = "")
            => OnEvent?.Invoke(new QuestEvent(type, id, pos, actorId, zone, kind));
    }
}
