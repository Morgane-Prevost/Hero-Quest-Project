using System;
using System.Collections.Generic;

namespace HeroQuest.Quests
{
    public class QuestContext
    {
        public QuestData Quest { get; }
        public IQuestWorld World { get; }
        public int Turn { get; set; }
        /// <summary>Héros à l'origine de l'événement en cours de traitement.</summary>
        public string CurrentActorId { get; set; } = "";

        readonly HashSet<string> flags = new();
        readonly HashSet<string> doneObjectives = new();
        readonly HashSet<string> firedRules = new();
        readonly HashSet<string> escaped = new();
        readonly Dictionary<string, int> kills = new();
        bool treasureHandled;

        public event Action<QuestObjective> OnObjectiveCompleted;

        public QuestContext(QuestData quest, IQuestWorld world) { Quest = quest; World = world; }

        // Flags
        public bool HasFlag(string f) => flags.Contains(f);
        public void SetFlag(string f, bool value)
        {
            bool changed = value ? flags.Add(f) : flags.Remove(f);
            if (changed) QuestEvents.Raise(QuestEventType.StateChanged, f);
        }

        // Kills
        public int Kills(string id) => kills.TryGetValue(id, out var n) ? n : 0;
        public void AddKill(string id) { kills[id] = Kills(id) + 1; }

        // Objectifs
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

        // Règles déjà déclenchées (globalement ou par héros)
        public string RuleKey(QuestRule r) => r.oncePerActor ? r.ruleId + ":" + CurrentActorId : r.ruleId;
        public bool RuleFired(QuestRule r) => firedRules.Contains(RuleKey(r));
        public void MarkRuleFired(QuestRule r) => firedRules.Add(RuleKey(r));

        // Évasions
        public int EscapedCount => escaped.Count;
        public bool MarkEscaped(string heroId) => escaped.Add(heroId);

        // Fouille de trésor : une règle peut "consommer" la fouille (pas de carte tirée dans la pioche)
        public void MarkTreasureHandled() => treasureHandled = true;
        public bool ConsumeTreasureHandled() { var v = treasureHandled; treasureHandled = false; return v; }

        // Destinataires (héros qui fouille / tous / un type de héros, ex. l'Enchanteur)
        public List<string> ResolveRecipients(RecipientType type, string heroClassId)
        {
            var list = new List<string>();
            switch (type)
            {
                case RecipientType.AllHeroes:
                    list.AddRange(World.HeroIds); break;
                case RecipientType.HeroClass:
                    var id = World.HeroIdOfClass(heroClassId);
                    if (!string.IsNullOrEmpty(id)) list.Add(id);
                    else return ResolveRecipients(RecipientType.Actor, heroClassId); // absent/mort -> à l'acteur
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
