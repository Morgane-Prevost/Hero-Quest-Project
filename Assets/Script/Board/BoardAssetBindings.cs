using System;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>Comment un prefab importé est adapté à la cellule / au segment visé.</summary>
    public enum PrefabFitMode
    {
        Native,          // taille d'origine, simplement recentré (X/Z) et posé sur Y=0
        UniformByLength, // échelle uniforme pour que la longueur (X) = cible, proportions conservées
        Stretch          // échelle par axe (X/Y/Z) ; un axe cible <= 0 garde sa taille native
    }

    /// <summary>
    /// Remplace un style de sol procédural par des assets importés.
    /// styleId = un id de floorStyles du BoardLayout.json.
    /// Priorité : tilePrefabs > materials > textures > PNG du JSON > procédural.
    /// </summary>
    [Serializable]
    public class FloorStyleBinding
    {
        public string styleId;
        [Tooltip("Dalle 3D (1 prefab par variante). Adaptée automatiquement à la taille d'une case.")]
        public GameObject[] tilePrefabs;
        [Tooltip("Matériaux prêts à l'emploi (1 par variante).")]
        public Material[] materials;
        [Tooltip("Textures : un matériau est créé à partir du Floor Base Material.")]
        public Texture[] textures;
    }

    /// <summary>
    /// Instancie un prefab importé et l'adapte à une taille cible grâce à ses Bounds.
    /// Retourne un GameObject « holder » : pivot au centre X/Z, base (ou sommet) à Y=0.
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