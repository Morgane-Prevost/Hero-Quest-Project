using System.Collections.Generic;
using UnityEngine;
using HeroQuest.Board;

namespace HeroQuest.Traps
{
    public enum TrapKind { Unknown, Pit, Rockfall, Spear }

    public enum CombatDieFace { Skull, WhiteShield, BlackShield }

    // Hidden -> Discovered (outline) -> Triggered (pit/rockfall tile down) -> Removed (disarmed or spent).
    public enum TrapPhase { Hidden, Discovered, Triggered, Removed }

    public enum TrapResult
    {
        NoTrap,       // nothing armed on the cell
        Triggered,    // the trap went off
        Avoided,      // spear trap: shield rolled, the hero keeps moving
        Disarmed,     // trap removed
        JumpedOver,   // the hero landed on the chosen cell
        JumpFailed,   // the hero fell into the trap (it went off)
        Moved,        // the hero left a rockfall cell
        Refused       // action not allowed, nothing changed (see Reason)
    }

    /// <summary>Everything the game loop needs to know after a trap action.</summary>
    public sealed class TrapOutcome
    {
        public TrapResult Result;
        public string TrapId;
        public TrapKind Kind;
        public int Damage;                 // body points lost (already applied through ITrapVictim)
        public bool TurnEnds;              // the hero's turn is over
        public int MovementSpent;          // movement points consumed by a jump
        public bool NeedsExitChoice;       // rockfall: the hero must pick one of ExitCells
        public bool Entombed;              // rockfall: no free cell around, the hero is stuck
        public Vector2Int LandingCell;
        public List<Vector2Int> ExitCells = new List<Vector2Int>();
        public List<CombatDieFace> Rolls = new List<CombatDieFace>();
        public string Message;             // text Zargon reads aloud
        public string Reason;              // why an action was refused
    }

    public struct TrapDiscoveredInfo
    {
        public string id; public TrapKind kind; public Vector2Int cell; public string message;
    }

    public sealed class TrapSearchResult
    {
        public bool Allowed;
        public string Reason;
        public List<string> TrapIds = new List<string>();        // newly found floor traps
        public List<string> FurnitureIds = new List<string>();   // trapped chests/furniture flagged as dangerous
    }

    /// <summary>What the trap rules need to know about a hero. Implemented by the game's own hero class.</summary>
    public interface ITrapVictim
    {
        string Id { get; }
        GridPawn Pawn { get; }
        bool IsDwarf { get; }
        bool HasToolkit { get; }
        /// <summary>Remove body points through the game's own health system.</summary>
        void ApplyDamage(int bodyPoints, string reason);
    }

    /// <summary>Game state the trap rules depend on. Implemented by the game loop.</summary>
    public interface ITrapGameContext
    {
        /// <summary>Increases at the start of every hero turn ("next turn" rules).</summary>
        int TurnId { get; }
        bool IsMonsterInSight(Vector2Int heroCell);
        bool HasLineOfSight(Vector2Int from, Vector2Int to);
        CombatDieFace RollCombatDie();
    }

    /// <summary>
    /// Stand-alone context used until the game loop provides its own:
    /// random die (3 skulls, 2 white shields, 1 black shield), no monster ever in sight, open line of sight.
    /// </summary>
    public sealed class BasicTrapContext : ITrapGameContext
    {
        public int TurnId { get; set; }
        public bool IsMonsterInSight(Vector2Int heroCell) { return false; }
        public bool HasLineOfSight(Vector2Int from, Vector2Int to) { return true; }

        public CombatDieFace RollCombatDie()
        {
            int r = Random.Range(0, 6);
            return r < 3 ? CombatDieFace.Skull : (r < 5 ? CombatDieFace.WhiteShield : CombatDieFace.BlackShield);
        }
    }

    /// <summary>Texts read by Zargon, taken from the rulebook. Replace here to localize.</summary>
    public static class TrapMessages
    {
        public const string MonsterInSight = "Cannot search for traps: a monster is in sight.";
        public const string HeroNotPlaced = "The hero is not placed on the board.";
        public const string PitDiscovered = "The floor looks unstable here.";
        public const string RockfallDiscovered = "The ceiling looks unstable here.";
        public const string SpearDiscovered = "This square looks suspicious.";
        public const string FurnitureDiscovered = "This chest or furniture looks dangerous.";
        public const string PitTriggered = "You have fallen into a pit trap!";
        public const string RockfallTriggered = "You have triggered a falling block trap! The ceiling collapses!";
        public const string SpearTriggered = "A spear thrusts out from the wall, floor, or ceiling!";
        public const string SpearHit = "It hits you.";
        public const string SpearAvoided = "You avoid it.";
        public const string Disarmed = "The trap is disarmed.";
        public const string JumpSuccess = "You jump over the trap.";
        public const string JumpFailed = "The jump fails: you fall into the trap!";
        public const string Entombed = "No open squares around you: you are trapped by the cave-in.";
        public const string NoTrapHere = "There is no trap on this square.";
        public const string TrapNotKnown = "The location of this trap is unknown.";
        public const string CannotDisarmTriggered = "A trap that has already been triggered cannot be disarmed.";
        public const string ToolkitRequired = "A tool kit is required (except for the dwarf).";
        public const string DisarmNextTurn = "A discovered trap can only be disarmed on a subsequent turn.";
        public const string CannotJumpRockfall = "Cannot jump over a falling block.";
        public const string NotEnoughMovement = "At least 2 movement points are required to jump.";
        public const string NoLanding = "No open square to land on.";
        public const string RockfallBlocked = "This square is blocked by the falling block.";
        public const string NoPendingDecision = "No pending cave-in exit decision for this hero.";
        public const string InvalidExit = "Invalid exit square.";


        public static string Discovered(TrapKind kind)
        {
            switch (kind)
            {
                case TrapKind.Pit: return PitDiscovered;
                case TrapKind.Rockfall: return RockfallDiscovered;
                default: return SpearDiscovered;
            }
        }
    }
}