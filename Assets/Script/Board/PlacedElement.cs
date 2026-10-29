using UnityEngine;

namespace HeroQuest.Board
{
    // Visual state of a floor trap. States only progress.
    // Removed = disarmed, or spent (a spear trap that already fired): every visual is gone.
    public enum TrapState { Hidden = 0, Highlighted = 1, Triggered = 2, Removed = 3 }

    /// <summary>Scene instance of a level element (door, furniture, monster, trap...).</summary>
    public class PlacedElement : MonoBehaviour
    {
        public ElementDef Def { get; private set; }
        public bool VisibleToHeroes { get; internal set; }
        public bool IsOpen { get; internal set; }
        public TrapState Trap { get; internal set; }
        public GridPawn Pawn { get; internal set; }        // monsters only

        internal GameObject visual;       // main visual
        internal GameObject marker;       // trap: gold marker (Zargon only)
        internal GameObject highlight;    // trap: suspicious cell (without tile)
        internal GameObject tile;         // trap: tile placed upon triggering
        internal Animator animator;       // optional: boolean parameter "Open"

        internal void Init(ElementDef def) { Def = def; }
    }
}