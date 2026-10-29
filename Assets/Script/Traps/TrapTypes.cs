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
        public const string MonsterInSight = "Impossible de chercher des pièges : un monstre est en vue.";
        public const string HeroNotPlaced = "Le héros n'est pas placé sur le plateau.";
        public const string PitDiscovered = "Le sol semble instable à cet endroit.";
        public const string RockfallDiscovered = "Le plafond semble instable à cet endroit.";
        public const string SpearDiscovered = "Cette case semble suspecte.";
        public const string FurnitureDiscovered = "Ce coffre ou ce meuble a l'air dangereux.";
        public const string PitTriggered = "Vous venez de tomber dans une oubliette !";
        public const string RockfallTriggered = "Vous venez de déclencher un piège à éboulement ! Le plafond s'effondre !";
        public const string SpearTriggered = "Une lance surgit du mur, du sol ou du plafond !";
        public const string SpearHit = "Elle vous touche.";
        public const string SpearAvoided = "Vous l'évitez.";
        public const string Disarmed = "Le piège est désarmé.";
        public const string JumpSuccess = "Vous sautez par-dessus le piège.";
        public const string JumpFailed = "Le saut échoue : vous tombez dans le piège !";
        public const string Entombed = "Plus aucune case libre autour de vous : vous êtes prisonnier de l'éboulement.";
        public const string NoTrapHere = "Il n'y a aucun piège sur cette case.";
        public const string TrapNotKnown = "L'emplacement de ce piège n'est pas connu.";
        public const string CannotDisarmTriggered = "Un piège déjà déclenché ne peut plus être désarmé.";
        public const string ToolkitRequired = "Une trousse à outils est nécessaire (sauf pour le nain).";
        public const string DisarmNextTurn = "Un piège découvert ne peut être désarmé qu'à un tour suivant.";
        public const string CannotJumpRockfall = "Impossible de sauter par-dessus un éboulement.";
        public const string NotEnoughMovement = "Il faut au moins 2 points de déplacement pour sauter.";
        public const string NoLanding = "Aucune case libre où atterrir.";
        public const string RockfallBlocked = "Cette case est obstruée par l'éboulement.";
        public const string NoPendingDecision = "Aucune sortie d'éboulement en attente pour ce héros.";
        public const string InvalidExit = "Case de sortie invalide.";

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