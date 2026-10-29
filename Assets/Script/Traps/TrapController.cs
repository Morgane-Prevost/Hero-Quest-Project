using System;
using System.Collections.Generic;
using UnityEngine;
using HeroQuest.Board;

namespace HeroQuest.Traps
{
    /// <summary>
    /// Rules layer for floor traps (pit = "PitTrap", rockfall = "RockFall", spear = "SpearTrap").
    /// It owns the state of every trap of the current level, applies the rulebook mechanics, and updates the
    /// visuals through LevelBuilder. Dice, hit points and turn order come from the game through
    /// ITrapGameContext / ITrapVictim. Pawns are moved here only for jump landings and rockfall exits.
    /// </summary>
    public class TrapController : MonoBehaviour
    {
        [Header("References")]
        public BoardBuilder board;
        public LevelBuilder level;

        [Header("Rules")]
        [Tooltip("Body points lost when falling into a pit (also when walking into an already open pit).")]
        public int pitFallDamage = 1;
        [Tooltip("Combat dice rolled when a rockfall goes off (each skull = 1 body point).")]
        public int rockfallDice = 3;
        [Tooltip("Movement points a hero needs left to jump over a trap.")]
        public int jumpMovementCost = 2;

        /// <summary>Game loop adapter. If left null, a BasicTrapContext is created.</summary>
        public ITrapGameContext Context { get; set; }

        public event Action<TrapDiscoveredInfo> TrapDiscovered;
        public event Action<ElementDef> FurnitureTrapDiscovered;
        /// <summary>Raised for every action that changed something (never for NoTrap / Refused).</summary>
        public event Action<TrapOutcome> TrapResolved;

        sealed class TrapRuntime
        {
            public ElementDef def;
            public TrapKind kind;
            public TrapPhase phase;
            public Vector2Int cell;
            public int discoveredTurn = int.MinValue;
        }

        static readonly Vector2Int[] Dirs = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

        readonly Dictionary<string, TrapRuntime> traps = new Dictionary<string, TrapRuntime>();
        readonly Dictionary<Vector2Int, TrapRuntime> byCell = new Dictionary<Vector2Int, TrapRuntime>();
        readonly Dictionary<string, Vector2Int> pendingRockfall = new Dictionary<string, Vector2Int>(); // hero id -> trap cell
        readonly HashSet<string> dangerousFurniture = new HashSet<string>();

        ITrapGameContext Ctx { get { return Context ?? (Context = new BasicTrapContext()); } }

        void Reset()
        {
            board = GetComponent<BoardBuilder>();
            level = GetComponent<LevelBuilder>();
        }

        void OnEnable() { if (level != null) level.LevelBuilt += OnLevelBuilt; }
        void OnDisable() { if (level != null) level.LevelBuilt -= OnLevelBuilt; }
        void Start() { if (level != null && level.Model != null) Rebuild(); }
        void OnLevelBuilt(LevelBuilder lb) { Rebuild(); }

        // Reads every trap of the level; all of them start hidden.
        void Rebuild()
        {
            traps.Clear(); byCell.Clear(); pendingRockfall.Clear(); dangerousFurniture.Clear();
            if (level == null || level.Model == null) return;
            foreach (var e in level.Model.Elements)
            {
                if (e.kind != ElementKind.Trap || e.cells.Count == 0) continue;
                TrapKind k = ParseKind(e.type);
                if (k == TrapKind.Unknown)
                {
                    Debug.LogWarning("[Traps] Type de piège inconnu '" + e.type + "' (" + e.id + ") : PitTrap, RockFall ou SpearTrap attendus.");
                    continue;
                }
                var t = new TrapRuntime { def = e, kind = k, phase = TrapPhase.Hidden, cell = e.cells[0] };
                traps[e.id] = t;
                byCell[t.cell] = t;
            }
        }

        static TrapKind ParseKind(string type)
        {
            if (string.IsNullOrEmpty(type)) return TrapKind.Unknown;
            string s = type.Replace(" ", "").Replace("_", "").ToLowerInvariant();
            if (s == "pittrap" || s == "pit") return TrapKind.Pit;
            if (s == "rockfall" || s == "rockfalltrap") return TrapKind.Rockfall;
            if (s == "speartrap" || s == "spear") return TrapKind.Spear;
            return TrapKind.Unknown;
        }

        // =====================================================================
        //  Queries for pathfinding / combat / UI
        // =====================================================================
        public TrapPhase GetPhase(string trapId)
        {
            TrapRuntime t; return traps.TryGetValue(trapId, out t) ? t.phase : TrapPhase.Removed;
        }

        /// <summary>The armed or triggered trap on a cell (false if none).</summary>
        public bool TryGetTrapAt(Vector2Int cell, out string id, out TrapKind kind, out TrapPhase phase)
        {
            TrapRuntime t = FindAt(cell);
            id = t != null ? t.def.id : null;
            kind = t != null ? t.kind : TrapKind.Unknown;
            phase = t != null ? t.phase : TrapPhase.Removed;
            return t != null;
        }

        /// <summary>A triggered rockfall is an obstacle for everybody, forever.</summary>
        public bool IsImpassable(int x, int y)
        {
            TrapRuntime t = FindAt(new Vector2Int(x, y));
            return t != null && t.kind == TrapKind.Rockfall && t.phase == TrapPhase.Triggered;
        }

        /// <summary>Combat dice lost by anybody (hero or monster) standing in an open pit. Apply a minimum of 1 die yourself.</summary>
        public int CombatDicePenalty(Vector2Int cell)
        {
            TrapRuntime t = FindAt(cell);
            return (t != null && t.kind == TrapKind.Pit && t.phase == TrapPhase.Triggered) ? 1 : 0;
        }

        /// <summary>True when a one-cell step is possible: no wall (or an open door), enterable and free target cell.</summary>
        public bool CanStep(Vector2Int from, Vector2Int to)
        {
            return EdgeClear(from, to) && CellOpen(to, true);
        }

        // =====================================================================
        //  Action 5: search for traps
        // =====================================================================
        /// <summary>
        /// "Je cherche des pièges." Refused while a monster is in sight. Every hidden trap of the hero's room
        /// (or of the corridor part he can see) becomes "discovered": outlined, without any tile.
        /// </summary>
        public TrapSearchResult SearchForTraps(ITrapVictim hero)
        {
            var res = new TrapSearchResult();
            if (!HeroReady(hero)) { res.Reason = TrapMessages.HeroNotPlaced; return res; }
            Vector2Int hc = hero.Pawn.Cell;
            if (Ctx.IsMonsterInSight(hc)) { res.Reason = TrapMessages.MonsterInSight; return res; }
            res.Allowed = true;

            foreach (var t in traps.Values)
            {
                if (t.phase != TrapPhase.Hidden || !InSearchScope(hc, t.cell)) continue;
                t.phase = TrapPhase.Discovered;
                t.discoveredTurn = Ctx.TurnId;
                level.HighlightTrap(t.def.id);
                res.TrapIds.Add(t.def.id);
                if (TrapDiscovered != null)
                    TrapDiscovered(new TrapDiscoveredInfo { id = t.def.id, kind = t.kind, cell = t.cell, message = TrapMessages.Discovered(t.kind) });
            }

            // Trapped chests/furniture are only flagged here (their own effects are handled elsewhere).
            foreach (var e in level.Model.Elements)
            {
                if (e.kind != ElementKind.Furniture || !e.trapped || dangerousFurniture.Contains(e.id)) continue;
                if (!FurnitureInScope(hc, e)) continue;
                dangerousFurniture.Add(e.id);
                res.FurnitureIds.Add(e.id);
                if (FurnitureTrapDiscovered != null) FurnitureTrapDiscovered(e);
            }
            return res;
        }

        public bool IsFurnitureTrapDiscovered(string furnitureId) { return dangerousFurniture.Contains(furnitureId); }

        bool InSearchScope(Vector2Int heroCell, Vector2Int target)
        {
            int zh = board.Model.ZoneAt(heroCell.x, heroCell.y), zt = board.Model.ZoneAt(target.x, target.y);
            if (zh < 0 || zh != zt) return false;       // another room: not searchable (e.g. beyond a door)
            return zh > 0 || Ctx.HasLineOfSight(heroCell, target);   // all corridors are one zone: use line of sight
        }

        bool FurnitureInScope(Vector2Int heroCell, ElementDef e)
        {
            int zh = board.Model.ZoneAt(heroCell.x, heroCell.y);
            if (zh < 0 || !e.zones.Contains(zh)) return false;
            if (zh > 0) return true;
            foreach (var c in e.cells) if (Ctx.HasLineOfSight(heroCell, c)) return true;
            return false;
        }

        // =====================================================================
        //  Entering a cell (walking, or announced disarm attempt)
        // =====================================================================
        /// <summary>
        /// Call right AFTER the hero's pawn was placed on a new cell. Triggers an armed trap automatically.
        /// With disarmAttempt = true (the hero announced it, see CanAttemptDisarm) the hero tries to disarm instead.
        /// An invalid disarm attempt is treated as a normal walk onto the trap.
        /// </summary>
        public TrapOutcome OnHeroEntered(ITrapVictim hero, bool disarmAttempt = false)
        {
            return Emit(EnterCore(hero, disarmAttempt));
        }

        TrapOutcome EnterCore(ITrapVictim hero, bool disarmAttempt)
        {
            if (!HeroReady(hero)) return Refuse(TrapMessages.HeroNotPlaced);
            TrapRuntime t = FindAt(hero.Pawn.Cell);
            if (t == null) return new TrapOutcome { Result = TrapResult.NoTrap };

            if (t.phase == TrapPhase.Triggered)
            {
                if (t.kind == TrapKind.Pit)          // walking into an open pit: the hero falls in again
                {
                    TrapOutcome o = NewOutcome(t, TrapResult.Triggered);
                    Hurt(hero, o, pitFallDamage, "pit");
                    o.TurnEnds = true;
                    o.Message = TrapMessages.PitTriggered;
                    return o;
                }
                return Refuse(TrapMessages.RockfallBlocked);   // rubble is an obstacle: nobody may enter
            }

            string why;
            if (disarmAttempt && CanDisarmCore(hero, t, out why)) return AttemptDisarm(hero, t);
            return Trigger(hero, t, false);
        }

        // Applies the effect of a trap. automaticHit = the hero is hit without any avoidance roll.
        TrapOutcome Trigger(ITrapVictim hero, TrapRuntime t, bool automaticHit)
        {
            TrapOutcome o = NewOutcome(t, TrapResult.Triggered);
            switch (t.kind)
            {
                case TrapKind.Pit:
                    t.phase = TrapPhase.Triggered;
                    level.TriggerTrap(t.def.id);                       // tile under the hero's figure
                    Hurt(hero, o, pitFallDamage, "pit");
                    o.TurnEnds = true;
                    o.Message = TrapMessages.PitTriggered;
                    break;

                case TrapKind.Rockfall:
                    t.phase = TrapPhase.Triggered;
                    level.TriggerTrap(t.def.id);                       // from now on the cell is an obstacle
                    int skulls = 0;
                    for (int i = 0; i < rockfallDice; i++)
                    {
                        CombatDieFace d = Ctx.RollCombatDie();
                        o.Rolls.Add(d);
                        if (d == CombatDieFace.Skull) skulls++;
                    }
                    Hurt(hero, o, skulls, "rockfall");                  // no defense roll
                    o.TurnEnds = true;                                 // ends once the hero has moved off the cell
                    o.Message = TrapMessages.RockfallTriggered;
                    o.ExitCells = GetExitCells(t.cell);
                    if (o.ExitCells.Count > 0) { o.NeedsExitChoice = true; pendingRockfall[hero.Id] = t.cell; }
                    else { o.Entombed = true; o.Message += " " + TrapMessages.Entombed; }
                    break;

                case TrapKind.Spear:
                    bool hit = automaticHit;
                    if (!automaticHit)
                    {
                        CombatDieFace d = Ctx.RollCombatDie();
                        o.Rolls.Add(d);
                        hit = d == CombatDieFace.Skull;
                    }
                    if (hit) { Hurt(hero, o, 1, "spear"); o.TurnEnds = true; }
                    else o.Result = TrapResult.Avoided;
                    o.Message = TrapMessages.SpearTriggered + " " + (hit ? TrapMessages.SpearHit : TrapMessages.SpearAvoided);
                    Retire(t);                                         // a spear trap works once, and has no tile
                    break;
            }
            return o;
        }

        // =====================================================================
        //  Action 6: disarm a trap
        // =====================================================================
        /// <summary>
        /// Can this hero announce "I move onto the trap cell to disarm it"? Needs a known, untriggered trap,
        /// a toolkit (or the dwarf), and a turn later than the one of the discovery.
        /// </summary>
        public bool CanAttemptDisarm(ITrapVictim hero, Vector2Int trapCell, out string reason)
        {
            reason = null;
            TrapRuntime t = FindAt(trapCell);
            if (t == null) { reason = TrapMessages.NoTrapHere; return false; }
            return CanDisarmCore(hero, t, out reason);
        }

        bool CanDisarmCore(ITrapVictim hero, TrapRuntime t, out string reason)
        {
            reason = null;
            if (t.phase == TrapPhase.Triggered) { reason = TrapMessages.CannotDisarmTriggered; return false; }
            if (t.phase != TrapPhase.Discovered) { reason = TrapMessages.TrapNotKnown; return false; }
            if (!hero.IsDwarf && !hero.HasToolkit) { reason = TrapMessages.ToolkitRequired; return false; }
            if (Ctx.TurnId <= t.discoveredTurn) { reason = TrapMessages.DisarmNextTurn; return false; }
            return true;
        }

        TrapOutcome AttemptDisarm(ITrapVictim hero, TrapRuntime t)
        {
            CombatDieFace die = Ctx.RollCombatDie();
            // Toolkit: a skull sets the trap off. Dwarf: only the black shield does.
            bool setOff = hero.IsDwarf ? die == CombatDieFace.BlackShield : die == CombatDieFace.Skull;
            if (setOff)
            {
                TrapOutcome failed = Trigger(hero, t, true);
                failed.Rolls.Insert(0, die);
                return failed;
            }
            Retire(t);                                                  // a disarmed trap no longer exists
            TrapOutcome o = NewOutcome(t, TrapResult.Disarmed);
            o.Rolls.Add(die);
            o.Message = TrapMessages.Disarmed;
            return o;
        }

        // =====================================================================
        //  Jumping over a trap
        // =====================================================================
        /// <summary>Free cells where a hero (or monster) can land after jumping over trapCell from 'from'.</summary>
        public List<Vector2Int> GetJumpLandings(Vector2Int from, Vector2Int trapCell)
        {
            var res = new List<Vector2Int>();
            if (!EdgeClear(from, trapCell)) return res;
            foreach (var d in Dirs)
            {
                Vector2Int n = trapCell + d;
                if (n == from) continue;
                if (EdgeClear(trapCell, n) && CellOpen(n, true)) res.Add(n);
            }
            return res;
        }

        /// <summary>
        /// The hero jumps from 'from' over trapCell and lands on 'landing'. Needs 2+ movement points left, a known trap,
        /// and one combat die that is not a skull. A skull = the hero falls into the trap and his turn ends.
        /// A triggered rockfall cannot be jumped over; an open pit can.
        /// </summary>
        public TrapOutcome TryJump(ITrapVictim hero, Vector2Int from, Vector2Int trapCell, Vector2Int landing, int movementLeft)
        {
            if (!HeroReady(hero)) return Refuse(TrapMessages.HeroNotPlaced);
            if (hero.Pawn.Cell != from) return Refuse(TrapMessages.InvalidExit);
            TrapRuntime t = FindAt(trapCell);
            if (t == null) return Refuse(TrapMessages.NoTrapHere);
            if (t.phase == TrapPhase.Hidden) return Refuse(TrapMessages.TrapNotKnown);
            if (t.kind == TrapKind.Rockfall && t.phase == TrapPhase.Triggered) return Refuse(TrapMessages.CannotJumpRockfall);
            if (movementLeft < jumpMovementCost) return Refuse(TrapMessages.NotEnoughMovement);
            if (!GetJumpLandings(from, trapCell).Contains(landing)) return Refuse(TrapMessages.NoLanding);

            CombatDieFace die = Ctx.RollCombatDie();
            TrapOutcome o;
            if (die == CombatDieFace.Skull)
            {
                hero.Pawn.PlaceAt(trapCell);                           // the hero stops on the trap cell
                if (t.phase == TrapPhase.Triggered)                    // an already open pit
                {
                    o = NewOutcome(t, TrapResult.Triggered);
                    Hurt(hero, o, pitFallDamage, "pit");
                    o.TurnEnds = true;
                    o.Message = TrapMessages.PitTriggered;
                }
                else o = Trigger(hero, t, true);                       // pit / rockfall / spear go off
                o.Result = TrapResult.JumpFailed;
                o.Message = TrapMessages.JumpFailed + " " + o.Message;
            }
            else
            {
                hero.Pawn.PlaceAt(landing);
                TrapOutcome follow = EnterCore(hero, false);           // the landing cell may hold another trap
                if (follow.Result == TrapResult.NoTrap || follow.Result == TrapResult.Refused)
                {
                    o = NewOutcome(t, TrapResult.JumpedOver);
                    o.Message = TrapMessages.JumpSuccess;
                }
                else o = follow;
                o.MovementSpent = jumpMovementCost;
                o.LandingCell = landing;
            }
            o.Rolls.Insert(0, die);
            return Emit(o);
        }

        /// <summary>Monsters never trigger traps and always clear an open pit when they have the movement and a free landing cell.</summary>
        public bool MonsterJumpOver(GridPawn monster, Vector2Int trapCell, Vector2Int landing, int movementLeft)
        {
            TrapRuntime t = FindAt(trapCell);
            if (t == null || t.kind != TrapKind.Pit || t.phase != TrapPhase.Triggered) return false;
            if (monster == null || !monster.HasCell || movementLeft < jumpMovementCost) return false;
            if (!GetJumpLandings(monster.Cell, trapCell).Contains(landing)) return false;
            return monster.PlaceAt(landing);
        }

        // =====================================================================
        //  Rockfall: leaving the cell
        // =====================================================================
        /// <summary>Free cells around a rockfall cell where the hero may go (forward or back).</summary>
        public List<Vector2Int> GetExitCells(Vector2Int trapCell)
        {
            var res = new List<Vector2Int>();
            foreach (var d in Dirs)
            {
                Vector2Int n = trapCell + d;
                if (EdgeClear(trapCell, n) && CellOpen(n, true)) res.Add(n);
            }
            return res;
        }

        public bool HasPendingRockfallExit(string heroId) { return pendingRockfall.ContainsKey(heroId); }

        /// <summary>The hero picked where to go after a rockfall. His turn ends (a trap on the destination still goes off).</summary>
        public TrapOutcome ResolveRockfallExit(ITrapVictim hero, Vector2Int destination)
        {
            Vector2Int trapCell;
            if (hero == null || !pendingRockfall.TryGetValue(hero.Id, out trapCell)) return Refuse(TrapMessages.NoPendingDecision);
            if (!GetExitCells(trapCell).Contains(destination)) return Refuse(TrapMessages.InvalidExit);

            pendingRockfall.Remove(hero.Id);
            hero.Pawn.PlaceAt(destination);
            TrapOutcome follow = EnterCore(hero, false);
            TrapOutcome o = (follow.Result == TrapResult.NoTrap || follow.Result == TrapResult.Refused)
                ? new TrapOutcome { Result = TrapResult.Moved } : follow;
            o.LandingCell = destination;
            o.TurnEnds = true;
            return Emit(o);
        }

        // =====================================================================
        //  Helpers
        // =====================================================================
        static bool HeroReady(ITrapVictim hero)
        {
            return hero != null && hero.Pawn != null && hero.Pawn.HasCell;
        }

        TrapRuntime FindAt(Vector2Int c)
        {
            TrapRuntime t;
            return (byCell.TryGetValue(c, out t) && t.phase != TrapPhase.Removed) ? t : null;
        }

        void Retire(TrapRuntime t)
        {
            t.phase = TrapPhase.Removed;
            TrapRuntime cur;
            if (byCell.TryGetValue(t.cell, out cur) && cur == t) byCell.Remove(t.cell);
            level.RemoveTrap(t.def.id);
        }

        static TrapOutcome NewOutcome(TrapRuntime t, TrapResult r)
        {
            return new TrapOutcome { Result = r, TrapId = t.def.id, Kind = t.kind };
        }

        static TrapOutcome Refuse(string reason)
        {
            return new TrapOutcome { Result = TrapResult.Refused, Reason = reason };
        }

        static void Hurt(ITrapVictim hero, TrapOutcome o, int amount, string reason)
        {
            if (amount <= 0) return;
            o.Damage += amount;
            hero.ApplyDamage(amount, reason);
        }

        TrapOutcome Emit(TrapOutcome o)
        {
            if (o.Result != TrapResult.NoTrap && o.Result != TrapResult.Refused && TrapResolved != null) TrapResolved(o);
            return o;
        }

        // No wall between two adjacent cells, or a wall with an OPEN door in it.
        bool EdgeClear(Vector2Int a, Vector2Int b)
        {
            EdgeKey e;
            if (!EdgeKey.TryBetween(a, b, out e)) return false;
            if (!level.Model.HasWall(e)) return true;
            ElementDef door = level.Model.DoorAt(e);
            if (door == null) return false;
            PlacedElement pe = level.GetElement(door.id);
            return pe != null && pe.IsOpen;
        }

        // The cell exists, is part of the level, holds no furniture/obstacle/rockfall (and no pawn if requireFree).
        bool CellOpen(Vector2Int c, bool requireFree)
        {
            CellView cv = board.GetCell(c.x, c.y);
            if (cv == null || !cv.Playable) return false;
            if (level.Model.IsBlocked(c.x, c.y) || IsImpassable(c.x, c.y)) return false;
            return !requireFree || cv.IsFree;
        }
    }
}