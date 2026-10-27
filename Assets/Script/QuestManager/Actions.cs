using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Quests
{
    [CreateAssetMenu(menuName = "HeroQuest/Actions/Show Note")]
    public class ShowNoteAction : QuestAction
    {
        public string noteLetter;      // "A", "B"... -> titre "Note A"
        public string title = "Note";  // utilisé si pas de lettre
        [TextArea(3, 12)] public string text;
        public override void Execute(QuestContext c)
            => UI.NotePopupUI.Instance.Show(string.IsNullOrEmpty(noteLetter) ? title : $"Note {noteLetter}", text);
    }

    /// <summary>Butin donné par la quête (or + cartes SO) au héros qui fouille / à tous / à une classe.</summary>
    [CreateAssetMenu(menuName = "HeroQuest/Actions/Grant Loot")]
    public class GrantLootAction : QuestAction
    {
        public RecipientType recipient = RecipientType.Actor;
        public string heroClassId;                         // si recipient = HeroClass (ex : "wizard")
        public int gold;
        public List<ScriptableObject> cards = new();
        public bool replacesTreasureDraw = true;           // pas de carte tirée dans la pioche en plus
        public override void Execute(QuestContext c)
        {
            foreach (var id in c.ResolveRecipients(recipient, heroClassId))
            {
                if (gold > 0) c.World.GiveGold(id, gold);
                foreach (var card in cards) if (card != null) c.World.GiveCard(id, card);
            }
            if (replacesTreasureDraw) c.MarkTreasureHandled();
        }
    }

    /// <summary>La fouille est "consommée" par la quête sans butin (ex : trouver l'équipement confisqué).</summary>
    [CreateAssetMenu(menuName = "HeroQuest/Actions/Consume Treasure Search")]
    public class ConsumeTreasureSearchAction : QuestAction
    { public override void Execute(QuestContext c) => c.MarkTreasureHandled(); }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Restore Equipment")]
    public class RestoreEquipmentAction : QuestAction
    {
        public RecipientType recipient = RecipientType.Actor;
        public override void Execute(QuestContext c)
        { foreach (var id in c.ResolveRecipients(recipient, "")) c.World.RestoreHeroEquipment(id); }
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Spawn Monsters")]
    public class SpawnMonstersAction : QuestAction
    {
        [Serializable] public struct Entry { public string monsterId; public Vector2Int pos; }
        public List<Entry> monsters = new();
        public override void Execute(QuestContext c)
        { foreach (var m in monsters) c.World.SpawnMonster(m.monsterId, m.pos); }
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Spawn Object")]
    public class SpawnObjectAction : QuestAction
    {
        public string objectId; public Vector2Int pos;
        public override void Execute(QuestContext c) => c.World.SpawnObject(objectId, pos);
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Set Door")]
    public class SetDoorAction : QuestAction
    {
        public string doorId; public bool open = true;
        public override void Execute(QuestContext c) => c.World.SetDoorOpen(doorId, open);
    }

    [CreateAssetMenu(menuName = "HeroQuest/Actions/Reveal Room")]
    public class RevealRoomAction : QuestAction
    {
        public string roomId;
        public override void Execute(QuestContext c) => c.World.RevealRoom(roomId);
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
}
