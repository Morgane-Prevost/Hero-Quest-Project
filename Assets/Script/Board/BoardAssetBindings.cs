using System;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>How an imported prefab is fitted to the target cell / segment.</summary>
    public enum PrefabFitMode
    {
        Native,         // original size, simply recentered (X/Z) and placed at Y=0
        UniformByLength, // uniform scale so length (X) = target, proportions preserved
        Stretch         // per-axis scale (X/Y/Z); a target axis <= 0 keeps its native size
    }

    /// <summary>
    /// Replaces a procedural floor style with imported assets.
    /// styleId = a floorStyles id from BoardLayout.json.
    /// Priority: tilePrefabs > materials > textures > JSON PNG > procedural.
    /// </summary>
    [Serializable]
    public class FloorStyleBinding
    {
        public string styleId;
        [Tooltip("3D tile (1 prefab per variant). Automatically fitted to the size of a cell.")]
        public GameObject[] tilePrefabs;
        [Tooltip("Ready-to-use materials (1 per variant).")]
        public Material[] materials;
        [Tooltip("Textures: a material is created from the Floor Base Material.")]
        public Texture[] textures;
    }

    /// <summary>
    /// Instantiates an imported prefab and fits it to a target size using its Bounds.
    /// Returns a "holder" GameObject: pivot at X/Z center, base (or top) at Y=0.
    /// </summary>
    public static class PrefabFit
    {
        public static GameObject Instantiate(GameObject prefab, Transform parent, Vector3 targetSize,
                                             PrefabFitMode mode, bool alignTop = false)
        {
            GameObject inst = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            Vector3 s0 = inst.transform.localScale;

            Bounds b;
            bool has = TryGetBounds(inst, out b);

            Vector3 k = Vector3.one;
            if (has && mode == PrefabFitMode.Stretch)
                k = new Vector3(Ratio(targetSize.x, b.size.x), Ratio(targetSize.y, b.size.y), Ratio(targetSize.z, b.size.z));
            else if (has && mode == PrefabFitMode.UniformByLength)
            {
                float u = Ratio(targetSize.x, b.size.x);
                k = new Vector3(u, u, u);
            }

            var holder = new GameObject(prefab.name + "_Fit").transform;
            holder.SetParent(parent, false);
            inst.transform.SetParent(holder, false);
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.Scale(s0, k);

            Vector3 c = has ? b.center : Vector3.zero;
            float y = has ? (alignTop ? b.max.y : b.min.y) : 0f;
            inst.transform.localPosition = new Vector3(-c.x * k.x, -y * k.y, -c.z * k.z);
            return holder.gameObject;
        }

        static float Ratio(float target, float size)
        {
            return (target <= 0f || size <= 1e-5f) ? 1f : target / size;
        }

        static bool TryGetBounds(GameObject go, out Bounds b)
        {
            b = new Bounds();
            bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any;
        }
    }
}