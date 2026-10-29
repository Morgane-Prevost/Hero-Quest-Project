using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>
    /// Pure placement tools for decor (no theme logic, no randomness).
    /// Everything it creates is "inert": no collider, no physics, so rules and raycasts never see it.
    /// </summary>
    public static class DecorPlacer
    {
        /// <summary>
        /// Instantiates a prop on a cell, rescaled so its LARGEST dimension = sizeCells, standing on y.
        /// With wall sides: pushed against the wall(s) and facing the room. Null if the prefab has no mesh.
        /// </summary>
        public static Transform Place(BoardBuilder board, float wallThickness, GameObject prefab, Transform parent,
                                      Vector2Int cell, IList<Dir> sides, float sizeCells, float y, float freeYaw)
        {
            if (prefab == null || board == null) return null;

            GameObject go = Object.Instantiate(prefab, parent);
            go.name = prefab.name;
            MakeInert(go, board.BoardLayer);

            float yaw = freeYaw;
            if (sides != null && sides.Count > 0)
            {
                Vector2 f = Vector2.zero;
                foreach (Dir d in sides) f -= new Vector2(DirUtil.DX[(int)d], DirUtil.DY[(int)d]);
                yaw = Mathf.Atan2(f.x, f.y) * Mathf.Rad2Deg;   // front (+Z) looks toward the room
            }

            Transform t = go.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            t.localScale = Vector3.one;

            Bounds b = LocalBounds(go, parent);
            float biggest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (biggest < 1e-4f) { Object.Destroy(go); return null; }

            t.localScale = Vector3.one * (sizeCells * board.tileSize / biggest);

            // center on the cell, bottom on y
            b = LocalBounds(go, parent);
            Vector3 c = board.CellToLocal(cell.x, cell.y);
            t.localPosition += new Vector3(c.x - b.center.x, y - b.min.y, c.z - b.center.z);

            // lean against the wall(s)
            if (sides != null && sides.Count > 0)
            {
                b = LocalBounds(go, parent);
                float gap = board.tileSize * 0.5f - wallThickness * 0.5f - 0.01f;
                foreach (Dir d in sides)
                {
                    int dx = DirUtil.DX[(int)d], dy = DirUtil.DY[(int)d];
                    if (dx != 0) t.localPosition += new Vector3((c.x + dx * gap) - (dx > 0 ? b.max.x : b.min.x), 0f, 0f);
                    if (dy != 0) t.localPosition += new Vector3(0f, 0f, (c.z + dy * gap) - (dy > 0 ? b.max.z : b.min.z));
                }
            }
            return t;
        }

        /// <summary>No collider, kinematic bodies, board layer: the rules never see the decor.</summary>
        public static void MakeInert(GameObject go, int layer)
        {
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        /// <summary>Renderer bounds of an object, expressed in the local space of 'space' (particles ignored).</summary>
        public static Bounds LocalBounds(GameObject go, Transform space)
        {
            bool first = true;
            Bounds res = new Bounds();
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                Bounds wb = r.bounds;
                Vector3 e = wb.extents, ctr = wb.center;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = ctr + new Vector3((i & 1) == 0 ? -e.x : e.x,
                                                       (i & 2) == 0 ? -e.y : e.y,
                                                       (i & 4) == 0 ? -e.z : e.z);
                    Vector3 p = space.InverseTransformPoint(corner);
                    if (first) { res = new Bounds(p, Vector3.zero); first = false; }
                    else res.Encapsulate(p);
                }
            }
            return res;
        }
    }
}