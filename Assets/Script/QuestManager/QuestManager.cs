using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Quests
{
    public enum QuestResult { Victory, Defeat }

    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Tooltip("Component implementing IQuestWorld (HeroQuestWorldBridge)")]
        [SerializeField] MonoBehaviour worldBehaviour;

        IQuestWorld world;
        QuestContext ctx;
        readonly Queue<QuestEvent> queue = new Queue<QuestEvent>();
        bool processing, running;

        public QuestContext Context => ctx;
        public bool IsRunning => running;
        public event Action<QuestData> OnQuestStarted;
        public event Action<QuestResult, QuestData> OnQuestEnded;

        void Awake()
        {
            Instance = this;
            world = worldBehaviour as IQuestWorld;
            if (world == null) Debug.LogError("[Quest] worldBehaviour must implement IQuestWorld (HeroQuestWorldBridge).");
        }

        void OnEnable()  => QuestEvents.OnEvent += Enqueue;
        void OnDisable() => QuestEvents.OnEvent -= Enqueue;

        // ───────── API called by the game ─────────

        /// <summary>Call BEFORE allowing a treasure search (forbidden in the cell, quest 6).</summary>
        public bool CanSearchTreasure(int zone)
            => ctx == null || !ctx.Quest.noTreasureZones.Contains(zone);

        /// <summary>
        /// Call when a hero searches for treasure. Returns true if the quest already resolved the search
        /// (fixed loot, empty chest...): then do NOT draw a card from the treasure deck.
        /// </summary>
        public bool NotifyTreasureSearch(int zone, string heroId, Vector2Int cell)
        {
            if (!running) return false;
            ctx.ConsumeTreasureHandled();                       // reset
            QuestEvents.Raise(QuestEventType.TreasureSearched, "", cell, heroId, zone);
            return ctx.ConsumeTreasureHandled();
        }

        /// <summary>Call when a trap is disarmed (furniture/trap system). Sets the flag "disarmed:&lt;elementId&gt;".</summary>
        public void NotifyTrapDisarmed(string elementId, string heroId)
        {
            if (!running) return;
            ctx.SetFlag("disarmed:" + elementId, true);
            QuestEvents.Raise(QuestEventType.TrapDisarmed, elementId, default, heroId);
        }

        public void ForceEnd(QuestResult result) => End(result);

        // ───────── Start ─────────
        public void StartQuest(QuestData quest)
        {
            if (world == null) { Debug.LogError("[Quest] No IQuestWorld: quest not started."); return; }
            if (quest.levelJson == null) Debug.LogWarning("[Quest] '" + quest.name + "' has no level JSON assigned.");

            queue.Clear();
            ctx = new QuestContext(quest, world);
            running = true;

            world.BeginQuest();
            world.LoadLevel(quest.levelJson);
            world.SetWanderingMonster(quest.wanderingMonsterType, quest.wanderingMonsterNote);
            world.PlaceHeroes(quest);
            if (quest.startEquipmentLocked)
                world.LockHeroEquipment(quest.unarmedAttackDice, quest.unarmedDefenseDice);

            OnQuestStarted?.Invoke(quest);
            if (!string.IsNullOrWhiteSpace(quest.introNote))
                QuestUtil.Note(quest.introTitle, quest.introNote);

            QuestEvents.Raise(QuestEventType.QuestStarted);
        }

        // ───────── Event loop (queue = no re-entrance) ─────────
        void Enqueue(QuestEvent e)
        {
            if (!running) return;
            queue.Enqueue(e);
            if (processing) return;
            processing = true;
            while (queue.Count > 0 && running) Process(queue.Dequeue());
            processing = false;
        }

        void Process(QuestEvent e)
        {
            ctx.CurrentActorId = e.ActorId;
            ctx.CurrentEvent = e;

            switch (e.Type)
            {
                case QuestEventType.MonsterKilled:
                    ctx.AddKill(e.Id, e.Kind);
                    PayBounty(e);
                    break;
                case QuestEventType.TurnStarted:
                    ctx.Turn++;
                    break;
                case QuestEventType.HeroMoved:
                    OnHeroMoved(e);
                    break;
            }

            foreach (var rule in ctx.Quest.rules)
            {
                bool limited = rule.once || rule.oncePerActor;
                if (limited && ctx.RuleFired(rule)) continue;
                if (!AllTrue(rule, e)) continue;

                ctx.MarkRuleFired(rule);
                foreach (var a in rule.actions) if (a != null) a.Execute(ctx);
                if (!running) return;
            }

            CheckEnd();
        }

        bool AllTrue(QuestRule rule, QuestEvent e)
        {
            if (rule.conditions.Count == 0) return false;
            foreach (var c in rule.conditions) if (c == null || !c.Evaluate(ctx, e)) return false;
            return true;
        }

        void PayBounty(QuestEvent e)
        {
            if (string.IsNullOrEmpty(e.ActorId)) return;
            foreach (var b in ctx.Quest.killBounties)
                if (QuestUtil.SameName(b.monsterType, e.Kind)) world.GiveGold(e.ActorId, b.gold);
        }

        void OnHeroMoved(QuestEvent e)
        {
            if (string.IsNullOrEmpty(e.ActorId) || world.IsAlly(e.ActorId)) return;

            ctx.MarkVisited(e.ActorId, e.Pos);
            if (!world.IsHeroOnStairs(e.ActorId)) ctx.LeftStairs = true;

            // Escape mode (quest 6, "Each hero escapes"): reaching the stairs = leaving the board
            var q = ctx.Quest;
            if (q.exitMode != ExitMode.EachHeroEscapesViaStairs) return;
            if (!world.IsHeroOnStairs(e.ActorId)) return;
            if (!ctx.AllMandatoryObjectivesDone()) return;
            if (!ctx.MarkEscaped(e.ActorId)) return;

            world.RemoveHeroFromBoard(e.ActorId);
            QuestEvents.Raise(QuestEventType.HeroEscaped, "", e.Pos, e.ActorId, e.Zone);   // queued
        }

        // ───────── End of the quest ─────────
        void CheckEnd()
        {
            if (queue.Count > 0) return;        // finish the queued events (notes...) before concluding
            var q = ctx.Quest;

            if (q.exitMode == ExitMode.EachHeroEscapesViaStairs)
            {
                // Assumption: victory if at least one hero escaped (to confirm with your rules)
                if (world.HeroesOnBoardCount == 0)
                    End(ctx.EscapedCount > 0 ? QuestResult.Victory : QuestResult.Defeat);
                return;
            }

            if (world.HeroesOnBoardCount == 0) { End(QuestResult.Defeat); return; }

            // The quest begins and ends on the stairs: objectives done AND everybody back on the stairs
            if (ctx.LeftStairs && ctx.AllMandatoryObjectivesDone() && world.AllHeroesOnBoardOnStairs())
                End(QuestResult.Victory);
        }

        void End(QuestResult result)
        {
            if (!running) return;
            running = false;
            var q = ctx.Quest;

            if (result == QuestResult.Victory) PayRewards(q);

            string note = result == QuestResult.Victory ? q.victoryNote : q.defeatNote;
            if (!string.IsNullOrWhiteSpace(note))
                QuestUtil.Note(result == QuestResult.Victory ? "Victoire !" : "Défaite", note);

            OnQuestEnded?.Invoke(result, q);
        }

        void PayRewards(QuestData q)
        {
            bool paid = string.IsNullOrEmpty(q.rewardRequiresObjectiveId) || ctx.IsObjectiveDone(q.rewardRequiresObjectiveId);
            if (!paid) return;

            var ids = world.HeroIds;
            if (ids.Count == 0) return;
            int share = q.sharedGoldReward / ids.Count;            // remainder ignored
            int each = share + q.goldRewardPerHero;
            if (each <= 0) return;
            foreach (var id in ids) world.GiveGold(id, each);
        }
    }
}
