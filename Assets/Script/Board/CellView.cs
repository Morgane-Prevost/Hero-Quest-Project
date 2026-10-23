using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>Posé sur chaque dalle de sol : identifie la case pour le picking et les overlays (pièges...).</summary>
    public class CellView : MonoBehaviour
    {
        public int X, Y, Zone;
        public CellKind Kind;
        public Renderer FloorRenderer;

        public void Init(int x, int y, int zone, CellKind kind, Renderer r)
        { X = x; Y = y; Zone = zone; Kind = kind; FloorRenderer = r; }
    }
}