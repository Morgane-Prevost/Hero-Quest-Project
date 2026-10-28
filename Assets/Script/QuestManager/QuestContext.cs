using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Quests
{
    /// <summary>Runtime state of the quest in progress.</summary>
    public class QuestContext
    {
        public QuestData Quest { get; }
        public IQuestWorld World { get; }
        public int Turn { get; set; }

        /// <summary>Hero (or ally) who caused the event being processed.</summary>
        public string CurrentActorId { get; set; } = "";
        public QuestEvent CurrentEvent { get; set; }

        /// <summary>True once a hero has stepped off the stairs (prevents an instant victory at the start).</summary>
        public bool LeftStairs { get; set; }

        readonly HashSet<string> flags = new HashSet<string>();
        readonly HashSet<string> doneObjectives = new HashSet<string>();
        readonly HashSet<string> firedRules = new HashSet<string>();
        readonly HashSet<string> escaped = new HashSet<string>();
        readonly Dictionary<string, int> killsById = new Dictionary<string, int>();
        readonly Dictionary<string, int> killsByType = new Dictionary<string, int>();
        readonly Dictionary<string, HashSet<Vector2Int>> visited = new Dictionary<string, HashSet<Vector2Int>>();
        bool treasureHandled;

        public event Action<QuestObjective> OnObjectiveCompleted;

        public QuestContext(QuestData quest, IQuestWorld world) { Quest = quest; World = world; }

        // ── Flags ──
        public bool HasFlag(string f) => flags.Contains(f);
        public void SetFlag(string f, bool value)
        {
            bool changed = value ? flags.Add(f) : flags.Remove(f);
            if (changed) QuestEvents.Raise(QuestEventType.StateChanged, f);
        }

        // ── Kills ──
        public void AddKill(string elementId, string type)
        {
            if (!string.IsNullOrEmpty(elementId)) killsById[elementId] = KillsOfElement(elementId) + 1;
            string t = QuestUtil.Normalize(type);
            if (t.Length > 0) killsByType[t] = KillsOfType(type) + 1;
        }
        public int KillsOfElement(string id) => id != null && killsById.TryGetValue(id, out var n) ? n : 0;
        public int KillsOfType(string type) => killsByType.TryGetValue(QuestUtil.Normalize(type), out var n) ? n : 0;

        // ── Objectives ──
        public bool IsObjectiveDone(string id) => doneObjectives.Contains(id);
        public void CompleteObjective(string id)
        {
            if (!doneObjectives.Add(id)) return;
            var obj = Quest.objectives.Find(o => o.id == id);
            if (obj != null) OnObjectiveCompleted?.Invoke(obj);
            QuestEvents.Raise(QuestEventType.StateChanged, id);
        }
        public bool AllMandatoryObjectivesDone()
        {
            foreach (var o in Quest.objectives)
                if (!o.optional && !doneObjectives.Contains(o.id)) return false;
            return true;
        }

        // ── Rules already fired (globally or per hero) ──
        public string RuleKey(QuestRule r) => r.oncePerActor ? r.ruleId + ":" + CurrentActorId : r.ruleId;
        public bool RuleFired(QuestRule r) => firedRules.Contains(RuleKey(r));
        public void MarkRuleFired(QuestRule r) => firedRules.Add(RuleKey(r));

        // ── Escapes ──
        public int EscapedCount => escaped.Count;
        public bool MarkEscaped(string heroId) => escaped.Add(heroId);

        // ── Tiles walked by each hero (quest 12 rockfall) ──
        public void MarkVisited(string heroId, Vector2Int cell)
        {
            if (!visited.TryGetValue(heroId, out var set)) visited[heroId] = set = new HashSet<Vector2Int>();
            set.Add(cell);
        }
        public bool HasVisited(string heroId, Vector2Int cell) => visited.TryGetValue(heroId, out var s) && s.Contains(cell);

        // ── Treasure search: a rule can "consume" it (no card drawn from the deck) ──
        public void MarkTreasureHandled() => treasureHandled = true;
        public bool ConsumeTreasureHandled() { var v = treasureHandled; treasureHandled = false; return v; }

        // ── Who receives loot ──
        public List<string> ResolveRecipients(RecipientType type, HeroType heroType)
        {
            var list = new List<string>();
            switch (type)
            {
                case RecipientType.AllHeroes:
                    list.AddRange(World.HeroIds);
                    break;
                case RecipientType.SpecificHeroType:
                    string id = World.HeroIdOfType(heroType);
                    if (!string.IsNullOrEmpty(id)) list.Add(id);
                    else return ResolveRecipients(RecipientType.Actor, heroType);   // absent / dead -> to the actor
                    break;
                default:
                    if (!string.IsNullOrEmpty(CurrentActorId)) list.Add(CurrentActorId);
                    else list.AddRange(World.HeroIds);
                    break;
            }
            return list;
        }
    }
}
