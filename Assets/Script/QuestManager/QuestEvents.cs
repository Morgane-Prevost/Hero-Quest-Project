using System;
using UnityEngine;

namespace HeroQuest.Quests
{
    public enum QuestEventType
    {
        QuestStarted, RoomEntered, MonsterKilled, HeroMoved, HeroDied, HeroEscaped,
        ItemPickedUp, DoorOpened, TileSearched, TreasureSearched, TurnStarted, StateChanged
    }

    /// <summary>
    /// Id = roomId / monsterId / itemId / doorId selon le type.
    /// ActorId = le héros qui a provoqué l'événement (indispensable : "le premier héros qui...", "chaque héros doit...").
    /// </summary>
    public readonly struct QuestEvent
    {
        public readonly QuestEventType Type;
        public readonly string Id;
        public readonly Vector2Int Pos;
        public readonly string ActorId;
        public QuestEvent(QuestEventType type, string id = "", Vector2Int pos = default, string actorId = "")
        { Type = type; Id = id ?? ""; Pos = pos; ActorId = actorId ?? ""; }
    }

    public static class QuestEvents
    {
        public static event Action<QuestEvent> OnEvent;
        public static void Raise(QuestEventType type, string id = "", Vector2Int pos = default, string actorId = "")
            => OnEvent?.Invoke(new QuestEvent(type, id, pos, actorId));
    }
}
