using System;
using System.Collections.Generic;
using HeroQuest.Board;
using UnityEngine;

namespace HeroQuest.Quests
{
    /// <summary>
    /// Links the quest system to the real game: BoardBuilder / LevelBuilder, HeroManager, MonsterManager,
    /// TreasureManager, ArtifactManager. Put it on a scene object and assign the references.
    /// </summary>
    public class HeroQuestWorldBridge : MonoBehaviour, IQuestWorld
    {
        public static HeroQuestWorldBridge Instance { get; private set; }

        [SerializeField] private BoardBuilder board;
        [SerializeField] private LevelBuilder level;
        [SerializeField] private TreasureManager treasure;
        [SerializeField] private ArtifactManager artifacts;

        private string wanderingType, wanderingNote;
        private readonly HashSet<string> killedElementIds = new HashSet<string>();
        private readonly HashSet<string> pendingActivation = new HashSet<string>();
        private readonly HashSet<Hero> hooked = new HashSet<Hero>();
        private bool monsterEventsBound;

        /// <summary>A trap card ends the hero's turn: the turn system listens to this.</summary>
        public event Action<Hero> HeroTurnEndRequested;

        private void Awake() { Instance = this; }

        private void Start() { BindMonsterEvents(); }

        private void OnDestroy()
        {
            if (monsterEventsBound && MonsterManager.Instance != null)
            {
                MonsterManager.Instance.MonsterSpawned -= OnMonsterSpawned;
                MonsterManager.Instance.MonsterKilled -= OnMonsterKilled;
            }
        }

        private void BindMonsterEvents()
        {
            if (monsterEventsBound || MonsterManager.Instance == null) return;
            MonsterManager.Instance.MonsterSpawned += OnMonsterSpawned;
            MonsterManager.Instance.MonsterKilled += OnMonsterKilled;
            monsterEventsBound = true;
        }

        private QuestData CurrentQuest => QuestManager.Instance != null && QuestManager.Instance.Context != null
            ? QuestManager.Instance.Context.Quest : null;

        // ═══════════════ Setup ═══════════════
        public void BeginQuest()
        {
            BindMonsterEvents();
            killedElementIds.Clear();
            pendingActivation.Clear();
            if (HeroManager.Instance != null) HeroManager.Instance.ClearAllies();
            if (treasure != null) treasure.StartQuest();        // every treasure card back in the pile
            if (artifacts != null) artifacts.StartQuest();      // once-per-quest powers come back
        }

        public void LoadLevel(TextAsset levelJson)
        {
            if (board.Model == null) board.Build();
            if (levelJson != null) level.levelJson = levelJson;
            level.Build();      // MonsterManager listens to LevelBuilt and spawns the visible monsters
        }

        public void PlaceHeroes(QuestData quest)
        {
            var cells = new List<Vector2Int>();
            if (level.Model == null) { Debug.LogError("[Quest] No level built: cannot place heroes."); return; }

            if (quest.startMode == HeroStartMode.OnStairs)
                cells.AddRange(level.Model.StairsCells);
            else
                foreach (Vector2Int c in board.Model.RoomCells(quest.startZone))
                    if (!level.Model.IsBlocked(c.x, c.y)) cells.Add(c);

            if (cells.Count == 0) { Debug.LogError("[Quest] No free start cell (stairs / zone " + quest.startZone + ")."); return; }

            HeroManager hm = HeroManager.Instance;
            if (!hm.HasHeroes) hm.CreateCharacter(cells); else hm.PlaceHeroes(cells);
            foreach (Hero h in hm.Heroes) Hook(h);
        }

        public void SetWanderingMonster(string monsterType, string note)
        {
            wanderingType = monsterType;
            wanderingNote = note;
        }

        // ═══════════════ Hero events -> quest events ═══════════════
        private void Hook(Hero h)
        {
            if (h == null || !hooked.Add(h)) return;
            if (h.Pawn != null) h.Pawn.CellChanged += OnPawnCellChanged;
            h.Died += OnHeroDied;
        }

        private void OnPawnCellChanged(GridPawn pawn, Vector2Int from, Vector2Int to)
        {
            if (from.x < 0) return;                                 // first placement, not a move
            Hero hero = pawn.GetComponent<Hero>();
            if (hero == null) return;

            int zFrom = board.Model.ZoneAt(from.x, from.y);
            int zTo = board.Model.ZoneAt(to.x, to.y);
            QuestEvents.Raise(QuestEventType.HeroMoved, "", to, hero.Id, zTo);
            if (zTo != zFrom && zTo > 0 && !hero.IsAlly)
                QuestEvents.Raise(QuestEventType.RoomEntered, "", to, hero.Id, zTo);
            // DoorCrossed: to be raised by MovementSystem when a path crosses a door edge (quest 10).
        }

        private void OnHeroDied(Hero h)
        {
            Vector2Int cell = h.Cell;
            int zone = cell.x >= 0 ? board.Model.ZoneAt(cell.x, cell.y) : -1;
            QuestEvents.Raise(QuestEventType.HeroDied, h.IsAlly ? "ally" : "hero", cell, h.Id, zone);
            if (h.Pawn != null) h.Pawn.Remove();
        }

        // ═══════════════ Monster events ═══════════════
        private void OnMonsterSpawned(Monster m)
        {
            QuestData q = CurrentQuest;
            if (q == null) return;

            foreach (MonsterModifier mod in q.monsterModifiers)
                if (QuestUtil.SameName(mod.monsterType, m.monsterType))
                {
                    m.attackDices += mod.attackDiceBonus;
                    m.defenseDices += mod.defenseDiceBonus;
                    m.healthPoints += mod.bodyBonus;
                }

            if (!string.IsNullOrEmpty(m.elementId))
                foreach (NamedMonsterDef d in q.namedMonsters)
                    if (d.elementId == m.elementId) ApplyNamed(m, d);

            if (!string.IsNullOrEmpty(m.elementId) && pendingActivation.Remove(m.elementId)) m.Activate();
        }

        private static void ApplyNamed(Monster m, NamedMonsterDef d)
        {
            if (!string.IsNullOrEmpty(d.displayName)) m.monsterName = d.displayName;
            if (d.movement >= 0) m.movementCells = d.movement;
            if (d.attackDice >= 0) m.attackDices = d.attackDice;
            if (d.defenseDice >= 0) m.defenseDices = d.defenseDice;
            if (d.bodyPoints >= 0) m.healthPoints = d.bodyPoints;
            if (d.mindPoints >= 0) m.mindPoints = d.mindPoints;

            m.isDormant = d.dormant;
            m.invulnerable = d.invulnerable;
            m.invulnerableUntilActed = d.invulnerableUntilActed;
            m.vulnerableOnlyToArtifactId = d.vulnerableOnlyToArtifactId;
            m.immuneToAllSpells = d.immuneToAllSpells;
            m.spellImmunities = new List<string>(d.spellImmunities);
            m.spellIds = new List<string>(d.spellIds);
            m.castsSpellInsteadOfAttacking = d.castsSpellInsteadOfAttacking;
            m.spellsOncePerQuest = d.spellsOncePerQuest;
        }

        private void OnMonsterKilled(Monster m)
        {
            if (!string.IsNullOrEmpty(m.elementId)) killedElementIds.Add(m.elementId);
            Vector2Int cell = m.cell;
            int zone = cell.x >= 0 ? board.Model.ZoneAt(cell.x, cell.y) : -1;
            string actor = m.LastAttacker != null ? m.LastAttacker.Id : "";
            QuestEvents.Raise(QuestEventType.MonsterKilled, m.elementId, cell, actor, zone, m.monsterType);
        }

        // ═══════════════ Treasure search (called by Hero.LookForTreasureAction) ═══════════════
        public void SearchTreasure(Hero hero)
        {
            if (hero == null || !hero.HasCell) return;
            Vector2Int cell = hero.Cell;
            int zone = board.Model.ZoneAt(cell.x, cell.y);
            QuestManager q = QuestManager.Instance;

            if (q != null && !q.CanSearchTreasure(zone))
            { QuestUtil.Note("Fouille impossible", "Il est impossible de chercher des trésors ici."); return; }

            // 1) the quest notes first (fixed loot, empty chest...)
            if (q != null && q.NotifyTreasureSearch(zone, hero.Id, cell)) return;

            // 2) otherwise a card from the treasure deck
            if (treasure == null) { Debug.LogWarning("[Quest] No TreasureManager assigned."); return; }
            TreasureCardSO card = treasure.Draw();
            if (card == null) return;
            Debug.Log("[Treasure] " + hero.heroName + " draws: " + card.displayName);
            card.Resolve(new HeroTreasureContext(hero, this));
        }

        private sealed class HeroTreasureContext : ITreasureContext
        {
            private readonly Hero hero;
            private readonly HeroQuestWorldBridge bridge;
            public HeroTreasureContext(Hero hero, HeroQuestWorldBridge bridge) { this.hero = hero; this.bridge = bridge; }

            public void AddGold(int amount) => hero.gold += amount;
            public void AddPotion(PotionCardSO potion) => hero.AddCardToInventory(potion);
            public void LoseLife(int amount) => hero.TakeDamage(amount);
            public void EndHeroTurn() => bridge.HeroTurnEndRequested?.Invoke(hero);
            public void SpawnWanderingMonster() => bridge.SpawnWanderingMonsterNear(hero);
        }

        // ═══════════════ Wandering monster ═══════════════
        public void SpawnWanderingMonsterNear(Hero hero)
        {
            if (string.IsNullOrEmpty(wanderingType))
            {
                if (!string.IsNullOrWhiteSpace(wanderingNote)) QuestUtil.Note("Monstre errant", wanderingNote);
                return;
            }
            Vector2Int? cell = FindFreeCellNear(hero.Cell, 3);
            if (cell == null) { Debug.LogWarning("[Quest] No free cell for the wandering monster."); return; }
            if (MonsterManager.Instance.SpawnByType(wanderingType, cell.Value) != null)
                QuestUtil.Note("Monstre errant", "Un monstre errant apparaît : " + wanderingType + " !");
        }

        private Vector2Int? FindFreeCellNear(Vector2Int origin, int maxDistance)
        {
            var seen = new HashSet<Vector2Int> { origin };
            var queue = new Queue<KeyValuePair<Vector2Int, int>>();
            queue.Enqueue(new KeyValuePair<Vector2Int, int>(origin, 0));

            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                Vector2Int c = item.Key; int d = item.Value;

                if (d > 0)
                {
                    CellView view = board.GetCell(c.x, c.y);
                    if (view != null && view.IsFree && !level.Model.IsBlocked(c.x, c.y)) return c;
                }
                if (d >= maxDistance) continue;

                for (int i = 0; i < 4; i++)
                {
                    Vector2Int n = c + DirUtil.Offset((Dir)i);
                    if (seen.Contains(n) || !board.Model.IsWalkable(n.x, n.y) || WallBetween(c, n)) continue;
                    seen.Add(n);
                    queue.Enqueue(new KeyValuePair<Vector2Int, int>(n, d + 1));
                }
            }
            return null;
        }

        private bool WallBetween(Vector2Int a, Vector2Int b)
        {
            EdgeKey edge;
            if (!EdgeKey.TryBetween(a, b, out edge)) return true;
            ElementDef door = level.Model.DoorAt(edge);
            if (door != null)
            {
                PlacedElement pe = level.GetElement(door.id);
                return pe == null || !pe.IsOpen;
            }
            return level.Model.HasWall(edge) && !level.IsEdgeOpen(edge);
        }

        // ═══════════════ Level ═══════════════
        public void RevealZone(int zone) => level.RevealZone(zone);
        public void RevealElement(string elementId) => level.RevealElement(elementId);
        public void OpenDoor(string doorId) => level.OpenDoor(doorId);

        public void RevealEverything()
        {
            foreach (ElementDef d in level.Model.Elements)
                if (d.kind == ElementKind.Door) level.OpenDoor(d.id);
            for (int z = 1; z <= board.Model.roomCount; z++) level.RevealZone(z);
            foreach (ElementDef d in level.Model.Elements)
                if (d.kind != ElementKind.Door && d.kind != ElementKind.Trap) level.RevealElement(d.id);
        }

        public void SpawnMonster(string monsterType, Vector2Int cell)
            => MonsterManager.Instance.SpawnByType(monsterType, cell);

        public void ActivateMonster(string elementId)
        {
            foreach (Monster m in MonsterManager.Instance.ActiveMonsters)
                if (m != null && m.elementId == elementId) { m.Activate(); return; }
            pendingActivation.Add(elementId);       // not revealed yet: wakes up when it appears
        }

        public void SpawnAlly(AllyDef def, string controllerHeroId)
        {
            if (def.template == null) { Debug.LogError("[Quest] Ally '" + def.id + "' has no HeroData template."); return; }

            Vector2Int cell = def.spawnCell;
            CellView view = board.GetCell(cell.x, cell.y);
            if (view == null || !view.IsFree)
            {
                Vector2Int? alt = FindFreeCellNear(cell, 3);
                if (alt == null) { Debug.LogError("[Quest] No free cell for ally " + def.id); return; }
                cell = alt.Value;
            }
            Hero ally = HeroManager.Instance.CreateAlly(def.template, def.id, def.displayName, cell);
            if (ally == null) return;
            ally.ControllerId = controllerHeroId;
            Hook(ally);
        }

        public void BlockCells(IList<Vector2Int> cells)
        {
            // TODO: needs a runtime "add obstacle / wall" in LevelBuilder (rockfall of quest 12).
            Debug.LogWarning("[Quest] BlockCells not implemented yet (" + cells.Count + " cells).");
        }

        public bool IsElementInZone(string elementId, int zone)
        {
            if (level.Model == null || string.IsNullOrEmpty(elementId)) return false;
            ElementDef d = level.Model.GetElement(elementId);
            return d != null && d.zones.Contains(zone);
        }

        public int RemainingLevelMonsters
        {
            get
            {
                if (level == null || level.Model == null) return 0;
                int n = 0;
                foreach (ElementDef d in level.Model.Elements)
                    if (d.kind == ElementKind.Monster && !killedElementIds.Contains(d.id)) n++;
                return n;
            }
        }

        // ═══════════════ Heroes ═══════════════
        private static Hero GetHero(string id) => HeroManager.Instance != null ? HeroManager.Instance.GetById(id) : null;

        public IReadOnlyList<string> HeroIds
        {
            get
            {
                var ids = new List<string>();
                if (HeroManager.Instance != null)
                    foreach (Hero h in HeroManager.Instance.Heroes) if (h != null && !h.IsAlly && !h.IsDead) ids.Add(h.Id);   // living heroes (escaped included)
                return ids;
            }
        }

        public string HeroIdOfType(HeroType type)
        {
            Hero h = HeroManager.Instance != null ? HeroManager.Instance.GetByType(type) : null;
            return h != null && !h.IsDead && !h.HasEscaped ? h.Id : null;
        }

        public bool IsAlly(string id) { Hero h = GetHero(id); return h != null && h.IsAlly; }

        public int HeroesOnBoardCount
        {
            get
            {
                int n = 0;
                if (HeroManager.Instance != null)
                    foreach (Hero h in HeroManager.Instance.Heroes)
                        if (h != null && !h.IsAlly && !h.IsDead && !h.HasEscaped) n++;
                return n;
            }
        }

        public bool IsHeroOnStairs(string heroId)
        {
            Hero h = GetHero(heroId);
            if (h == null || !h.HasCell || level.Model == null) return false;
            foreach (Vector2Int c in level.Model.StairsCells) if (c == h.Cell) return true;
            return false;
        }

        public bool AllHeroesOnBoardOnStairs()
        {
            int count = 0;
            foreach (Hero h in HeroManager.Instance.Heroes)
            {
                if (h == null || h.IsAlly || h.IsDead || h.HasEscaped) continue;
                count++;
                if (!IsHeroOnStairs(h.Id)) return false;
            }
            return count > 0;
        }

        public void LockHeroEquipment(int attackDice, int defenseDice)
        { foreach (Hero h in HeroManager.Instance.Heroes) if (h != null && !h.IsAlly) h.QuestLockEquipment(attackDice, defenseDice); }

        public void RestoreHeroEquipment(string heroId) { Hero h = GetHero(heroId); if (h != null) h.QuestRestoreEquipment(); }
        public void GiveCard(string heroId, CardSO card) { Hero h = GetHero(heroId); if (h != null) h.AddCardToInventory(card); }
        public void GiveGold(string heroId, int amount) { Hero h = GetHero(heroId); if (h != null) h.gold += amount; }
        public void DamageHero(string heroId, int amount) { Hero h = GetHero(heroId); if (h != null) h.TakeDamage(amount); }
        public void LimitMovementDice(string heroId, int maxDice) { Hero h = GetHero(heroId); if (h != null) h.MovementDiceCap = maxDice; }
        public bool HeroWearsHelmet(string heroId) { Hero h = GetHero(heroId); return h != null && h.WearsHelmet; }
        public int RollRedDie() => ActionManager.Instance.RollRedDice();

        public void GrantArtifact(string heroId, string artifactId)
        {
            Hero h = GetHero(heroId);
            if (h == null || artifacts == null) { Debug.LogWarning("[Quest] Cannot grant artifact '" + artifactId + "'."); return; }
            artifacts.Grant(artifactId, h);
        }

        public void RemoveHeroFromBoard(string heroId)
        {
            Hero h = GetHero(heroId);
            if (h == null) return;
            h.MarkEscaped();
            if (h.Pawn != null) h.Pawn.Remove();
            h.gameObject.SetActive(false);
        }

        public bool TeleportHero(string heroId, Vector2Int cell)
        {
            Hero h = GetHero(heroId);
            return h != null && h.Pawn != null && h.Pawn.PlaceAt(cell);
        }

        public void TeleportHeroToStairs(string heroId)
        {
            Hero h = GetHero(heroId);
            if (h == null || h.Pawn == null) return;
            foreach (Vector2Int c in level.Model.StairsCells) if (h.Pawn.PlaceAt(c)) return;
        }
    }
}
