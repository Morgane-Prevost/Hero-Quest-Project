using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Quests
{
    public enum QuestResult { Victory, Defeat }

    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Tooltip("Composant qui implémente IQuestWorld (HeroQuestWorldBridge)")]
        [SerializeField] MonoBehaviour worldBehaviour;

        IQuestWorld world;
        QuestContext ctx;
        readonly Queue<QuestEvent> queue = new();
        bool processing, running;

        public QuestContext Context => ctx;
        public string WanderingMonsterId => ctx?.Quest.wanderingMonsterId;
        public event Action<QuestData> OnQuestStarted;
        public event Action<QuestResult, QuestData> OnQuestEnded;

        void Awake()
        {
            Instance = this;
            world = worldBehaviour as IQuestWorld;
            if (world == null) Debug.LogError("worldBehaviour doit implémenter IQuestWorld");
        }

        void OnEnable()  => QuestEvents.OnEvent += Enqueue;
        void OnDisable() => QuestEvents.OnEvent -= Enqueue;

        // ───────── API appelée par le jeu ─────────

        /// <summary>À appeler AVANT d'autoriser une fouille de trésor (ex : interdit dans la cellule).</summary>
        public bool CanSearchTreasure(string roomId)
            => ctx == null || !ctx.Quest.noTreasureRooms.Contains(roomId);

        /// <summary>
        /// À appeler quand un héros fouille une pièce. Retourne true si la quête a déjà donné le butin
        /// (note A/B...) : dans ce cas NE PAS tirer de carte trésor dans la pioche.
        /// </summary>
        public bool NotifyTreasureSearch(string roomId, string heroId, Vector2Int pos)
        {
            if (!running) return false;
            ctx.ConsumeTreasureHandled();                       // reset
            QuestEvents.Raise(QuestEventType.TreasureSearched, roomId, pos, heroId);
            return ctx.ConsumeTreasureHandled();
        }

        // ───────── Démarrage ─────────
        public void StartQuest(QuestData quest)
        {
            ctx = new QuestContext(quest, world);
            running = true;

            world.LoadMap(quest.mapJson);
            world.PlaceStairs(quest.stairsPosition);
            foreach (var m in quest.namedMonsters) world.ApplyNamedMonster(m);
            world.SetWanderingMonster(quest.wanderingMonsterId);
            if (quest.startEquipmentLocked)
                world.LockHeroEquipment(quest.unarmedAttackDice, quest.unarmedDefenseDice);
            world.PlaceHeroes(quest);     // NE PAS lever HeroMoved lors du placement initial

            OnQuestStarted?.Invoke(quest);
            if (!string.IsNullOrWhiteSpace(quest.introNote))
                UI.NotePopupUI.Instance.Show(quest.introTitle, quest.introNote);

            QuestEvents.Raise(QuestEventType.QuestStarted);
        }

        // ───────── Boucle d'événements (file d'attente = pas de réentrance) ─────────
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

            switch (e.Type)
            {
                case QuestEventType.MonsterKilled: ctx.AddKill(e.Id); break;
                case QuestEventType.TurnStarted:   ctx.Turn++; break;
                case QuestEventType.HeroMoved:     TryEscape(e); break;
            }

            foreach (var rule in ctx.Quest.rules)
            {
                bool limited = rule.once || rule.oncePerActor;
                if (limited && ctx.RuleFired(rule)) continue;
                if (!AllTrue(rule, e)) continue;

                ctx.MarkRuleFired(rule);
                foreach (var a in rule.actions) a.Execute(ctx);
            }

            CheckEnd();
        }

        bool AllTrue(QuestRule rule, QuestEvent e)
        {
            if (rule.conditions.Count == 0) return false;
            foreach (var c in rule.conditions) if (c == null || !c.Evaluate(ctx, e)) return false;
            return true;
        }

        // Mode "chaque héros s'échappe" (quête 6) : atteindre l'escalier = sortir du plateau
        void TryEscape(QuestEvent e)
        {
            var q = ctx.Quest;
            if (q.exitMode != ExitMode.EachHeroEscapesViaStairs) return;
            if (e.Pos != q.stairsPosition || string.IsNullOrEmpty(e.ActorId)) return;
            if (!ctx.AllMandatoryObjectivesDone()) return;
            if (!ctx.MarkEscaped(e.ActorId)) return;

            world.RemoveHeroFromBoard(e.ActorId);
            QuestEvents.Raise(QuestEventType.HeroEscaped, "", e.Pos, e.ActorId); // mis en file
        }

        // ───────── Fin de quête ─────────
        void CheckEnd()
        {
            if (queue.Count > 0) return;     // on finit de traiter les événements (notes...) avant de conclure
            var q = ctx.Quest;

            if (q.exitMode == ExitMode.EachHeroEscapesViaStairs)
            {
                // Hypothèse : victoire si au moins un héros s'est échappé (à confirmer selon tes règles)
                if (world.HeroesOnBoardCount == 0)
                    End(ctx.EscapedCount > 0 ? QuestResult.Victory : QuestResult.Defeat);
                return;
            }

            if (world.HeroesOnBoardCount == 0) { End(QuestResult.Defeat); return; }
            if (ctx.AllMandatoryObjectivesDone() && world.AllHeroesOnBoardOn(q.stairsPosition))
                End(QuestResult.Victory);
        }

        void End(QuestResult result)
        {
            if (!running) return;
            running = false;
            var q = ctx.Quest;

            if (result == QuestResult.Victory && q.sharedGoldReward > 0 && world.HeroIds.Count > 0)
            {
                int share = q.sharedGoldReward / world.HeroIds.Count;   // reste ignoré
                foreach (var id in world.HeroIds) world.GiveGold(id, share);
            }

            string note = result == QuestResult.Victory ? q.victoryNote : q.defeatNote;
            if (!string.IsNullOrWhiteSpace(note))
                UI.NotePopupUI.Instance.Show(result == QuestResult.Victory ? "Victoire !" : "Défaite", note);

            OnQuestEnded?.Invoke(result, q);
        }
    }
}
