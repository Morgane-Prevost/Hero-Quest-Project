using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HeroQuest.Board
{
    /// <summary>Level designer helper: reads cell coordinates for writing JSONs.</summary>
    public class BoardDebugOverlay : MonoBehaviour
    {
        public BoardBuilder board;
        public LevelBuilder level;                 // optional: also displays elements under the mouse
        public bool showCoordinatesInScene = true;    // Scene view (Gizmos icon enabled)
        public bool showMouseCell = true;             // Game view
        public Camera cam;                            // default: Camera.main

        CellView hovered;

        void Reset() { board = GetComponent<BoardBuilder>(); level = GetComponent<LevelBuilder>(); }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!showCoordinatesInScene || board == null || board.Model == null) return;
            var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
            for (int x = 0; x < board.Model.width; x++)
                for (int y = 0; y < board.Model.height; y++)
                {
                    if (board.Model.ZoneAt(x, y) < 0) continue;
                    Handles.Label(board.CellToWorld(x, y) + Vector3.up * 0.02f, x + "," + y, style);
                }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        void Update()
        {
            hovered = null;
            if (!showMouseCell) return;
            Camera c = cam != null ? cam : Camera.main;
            if (c == null) return;
            RaycastHit hit;
            if (Physics.Raycast(c.ScreenPointToRay(Input.mousePosition), out hit, 1000f))
                hovered = hit.collider.GetComponentInParent<CellView>();
        }
#endif

        void OnGUI()
        {
            if (!showMouseCell || hovered == null) return;
            string txt = "Cell (" + hovered.X + "," + hovered.Y + ")  zone " + hovered.Zone;
            if (hovered.Occupant != null) txt += "  pawn: " + hovered.Occupant.name;
            if (level != null && level.Model != null)
                foreach (var e in level.Model.ElementsAt(hovered.X, hovered.Y)) txt += "  [" + e.kind + " " + e.id + "]";
            GUI.Box(new Rect(10, 10, 560, 26), txt);
        }
    }
}