using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HeroQuest.Board
{
    /// <summary>
    /// Construit la GRID MAP : sol coloré 26x19, rien d'autre (ni murs, ni portes).
    /// Les murs et le contenu viennent de LevelBuilder.
    /// </summary>
    public class BoardBuilder : MonoBehaviour
    {
        [Header("Grid map (JSON)")]
        [Tooltip("GridMap.json du projet (Assets/Script/Board/Levels). Prioritaire.")]
        [FormerlySerializedAs("layoutJson")] public TextAsset gridMapJson;
        [Tooltip("Chemin relatif à StreamingAssets : utilisé seulement si Grid Map Json est vide (extensions).")]
        [FormerlySerializedAs("layoutPath")] public string gridMapPath = "";
        public bool buildOnStart = true;

        [Header("Géométrie (unités Unity)")]
        public float tileSize = 1f;
        public bool floorColliders = true;   // nécessaire au picking souris
        public bool randomizeFloorRotation = true; // à désactiver si les textures ont un éclairage « cuit »

        [Header("Sols importés (remplacent les placeholders procéduraux)")]
        [Tooltip("Matériau de base copié pour chaque style procédural / texture. Recommandé en build.")]
        public Material floorBaseMaterial;
        public List<FloorStyleBinding> floorStyleBindings = new List<FloorStyleBinding>();

        [Header("Calque des éléments de plateau")]
        public string boardLayerName = "Board"; // si absent : Default

        // ---- API publique ----
        public BoardModel Model { get; private set; }
        public GridMapData Data { get; private set; }
        public int BoardLayer { get { return layer; } }
        public event Action<BoardBuilder> BoardBuilt;
        public event Action BoardCleared;

        const string RootName = "BoardGenerated";

        CellView[,] cells;
        readonly Dictionary<string, Material[]> styleMats = new Dictionary<string, Material[]>();
        readonly Dictionary<string, FloorStyleBinding> bindings = new Dictionary<string, FloorStyleBinding>();
        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        string fallbackStyleId;
        int layer;

        static Mesh quadMesh;

#if UNITY_EDITOR
        void Reset()
        {
            foreach (var guid in AssetDatabase.FindAssets("GridMap t:TextAsset"))
            {
                var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (ta != null && ta.name == "GridMap") { gridMapJson = ta; break; }
            }
        }
#endif

        void Start() { if (buildOnStart) Build(); }
        void OnDestroy() { DestroyOwned(); }

        // ================== Coordonnées ==================
        public Vector3 CellToLocal(int x, int y) { return new Vector3((x + 0.5f) * tileSize, 0f, (y + 0.5f) * tileSize); }
        public Vector3 CellToWorld(int x, int y) { return transform.TransformPoint(CellToLocal(x, y)); }

        public Vector3 EdgeToLocal(EdgeKey e)
        {
            return e.horizontal
                ? new Vector3((e.x + 0.5f) * tileSize, 0f, e.y * tileSize)
                : new Vector3(e.x * tileSize, 0f, (e.y + 0.5f) * tileSize);
        }
        public Vector3 EdgeToWorld(EdgeKey e) { return transform.TransformPoint(EdgeToLocal(e)); }

        public bool WorldToCell(Vector3 world, out Vector2Int cell)
        {
            Vector3 l = transform.InverseTransformPoint(world);
            cell = new Vector2Int(Mathf.FloorToInt(l.x / tileSize), Mathf.FloorToInt(l.z / tileSize));
            return Model != null && Model.InBounds(cell.x, cell.y);
        }

        public CellView GetCell(int x, int y)
        {
            return (cells != null && Model != null && Model.InBounds(x, y)) ? cells[x, y] : null;
        }

        // ================== Construction ==================
        [ContextMenu("Build Grid Map")]
        public bool Build()
        {
            GridMapData data; string err;
            bool loaded = gridMapJson != null
                ? GridMapLoader.TryParse(gridMapJson.text, gridMapJson.name, out data, out err)
                : GridMapLoader.TryLoad(gridMapPath, out data, out err);
            if (!loaded) { Debug.LogError("[Board] " + err); return false; }
            return BuildFromData(data);
        }

        /// <summary>Pour le chargeur d'extensions (Mission 2).</summary>
        public bool BuildFromJson(string json, string sourceName = "extension")
        {
            GridMapData data; string err;
            if (!GridMapLoader.TryParse(json, sourceName, out data, out err)) { Debug.LogError("[Board] " + err); return false; }
            return BuildFromData(data);
        }

        public bool BuildFromData(GridMapData data)
        {
            Clear();

            var errors = new List<string>(); var warnings = new List<string>();
            BoardModel model;
            bool ok = GridMapLoader.TryBuildModel(data, out model, errors, warnings);
            foreach (var w in warnings) Debug.LogWarning("[Board] " + w);
            if (!ok) { foreach (var e in errors) Debug.LogError("[Board] " + e); return false; }

            Data = data; Model = model;
            layer = LayerMask.NameToLayer(boardLayerName); if (layer < 0) layer = 0;

            var root = new GameObject(RootName).transform;
            root.SetParent(transform, false);
            root.gameObject.layer = layer;

            BuildStyles();
            BuildFloors(NewChild(root, "Floors"));

            Debug.Log("[Board] Grid map OK : " + Model.width + "x" + Model.height + ", " + Model.roomCount + " salles.");
            if (BoardBuilt != null) BoardBuilt(this);
            return true;
        }

        [ContextMenu("Clear Grid Map")]
        public void Clear()
        {
            if (BoardCleared != null) BoardCleared(); // le LevelBuilder se vide aussi
            var t = transform.Find(RootName);
            if (t != null) { t.name = RootName + "_old"; SafeDestroy(t.gameObject); }
            DestroyOwned();
            cells = null;
            styleMats.Clear(); bindings.Clear();
            Model = null; Data = null;
        }

        void DestroyOwned()
        {
            foreach (var o in owned) if (o != null) SafeDestroy(o);
            owned.Clear();
        }

        static void SafeDestroy(UnityEngine.Object o)
        {
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        Transform NewChild(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.gameObject.layer = layer;
            return t;
        }

        // ---------- Styles de sol ----------
        static bool Any(UnityEngine.Object[] a)
        {
            if (a == null) return false;
            foreach (var o in a) if (o != null) return true;
            return false;
        }

        Material MakeFloorMaterial(Texture tex, string name)
        {
            var m = floorBaseMaterial != null ? new Material(floorBaseMaterial) : new Material(FloorStyleFactory.FindLitShader());
            m.mainTexture = tex; m.color = Color.white;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.08f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.08f);
            m.name = name;
            owned.Add(m);
            return m;
        }

        void BuildStyles()
        {
            var styles = Data.floorStyles;
            if (styles == null || styles.Length == 0) styles = new[] { new FloorStyleData() };
            fallbackStyleId = styles[0].id;

            bindings.Clear();
            foreach (var b in floorStyleBindings)
                if (b != null && !string.IsNullOrEmpty(b.styleId)) bindings[b.styleId] = b;

            var known = new HashSet<string>();
            var stillProcedural = new List<string>();

            foreach (var s in styles)
            {
                known.Add(s.id);
                FloorStyleBinding bind; bindings.TryGetValue(s.id, out bind);

                bool hasPrefab = bind != null && Any(bind.tilePrefabs);
                if (hasPrefab && s.id != fallbackStyleId) continue; // pas de matériaux inutiles

                var list = new List<Material>();
                if (bind != null && Any(bind.materials))
                {
                    foreach (var m in bind.materials) if (m != null) list.Add(m);   // matériaux de l'artiste (non détruits)
                }
                else if (bind != null && Any(bind.textures))
                {
                    int i = 0;
                    foreach (var t in bind.textures) if (t != null) list.Add(MakeFloorMaterial(t, "Floor_" + s.id + "_t" + (i++)));
                }
                else
                {
                    if (!hasPrefab) stillProcedural.Add(s.id);
                    int n = Mathf.Max(1, s.variants);
                    for (int v = 0; v < n; v++)
                    {
                        var tex = FloorStyleFactory.BuildTexture(s, v); owned.Add(tex);
                        list.Add(MakeFloorMaterial(tex, "Floor_" + s.id + "_v" + v));
                    }
                }
                styleMats[s.id] = list.ToArray();
            }

            foreach (var id in bindings.Keys)
                if (!known.Contains(id)) Debug.LogWarning("[Board] floorStyleBindings : styleId '" + id + "' absent du JSON (ignoré).");
            if (stillProcedural.Count > 0)
                Debug.Log("[Board] Styles encore procéduraux (placeholders) : " + string.Join(", ", stillProcedural.ToArray()));
        }

        string ResolveStyleId(int zone)
        {
            if (zone <= 0) return Data.corridorStyle;
            if (Data.roomOverrides != null)
                foreach (var o in Data.roomOverrides)
                    if (o.zone == zone && !string.IsNullOrEmpty(o.floorStyle)) return o.floorStyle;
            var p = Data.roomStylePalette;
            if (p != null && p.Length > 0) return p[(zone - 1) % p.Length];
            return fallbackStyleId;
        }

        Material[] GetMats(string id)
        {
            Material[] a;
            if (!string.IsNullOrEmpty(id) && styleMats.TryGetValue(id, out a) && a.Length > 0) return a;
            Debug.LogWarning("[Board] Style de sol inconnu '" + id + "' → style par défaut.");
            return styleMats[fallbackStyleId];
        }

        // ---------- Sols ----------
        void BuildFloors(Transform parent)
        {
            cells = new CellView[Model.width, Model.height];
            for (int x = 0; x < Model.width; x++)
                for (int y = 0; y < Model.height; y++)
                {
                    int zone = Model.ZoneAt(x, y);
                    if (zone < 0) continue;

                    string styleId = ResolveStyleId(zone);
                    FloorStyleBinding bind; bindings.TryGetValue(styleId ?? "", out bind);

                    GameObject go; Renderer mr;
                    Quaternion rot = randomizeFloorRotation
                        ? Quaternion.Euler(0f, 90f * (Hash(x, y, 1) % 4), 0f) : Quaternion.identity;

                    if (bind != null && Any(bind.tilePrefabs))
                    {
                        // Dalle 3D importée : adaptée à la case, face supérieure à Y=0
                        var valid = new List<GameObject>();
                        foreach (var p in bind.tilePrefabs) if (p != null) valid.Add(p);
                        var pf = valid[Hash(x, y, 2) % valid.Count];
                        go = PrefabFit.Instantiate(pf, parent, new Vector3(tileSize, 0f, tileSize), PrefabFitMode.Stretch, true);
                        go.transform.localPosition = CellToLocal(x, y);
                        go.transform.localRotation = rot;
                        SetLayerRecursive(go, layer);
                        mr = go.GetComponentInChildren<Renderer>();
                        if (floorColliders && go.GetComponentInChildren<Collider>() == null)
                        {
                            var bc = go.AddComponent<BoxCollider>();
                            bc.center = new Vector3(0f, -0.025f, 0f);
                            bc.size = new Vector3(tileSize, 0.05f, tileSize);
                        }
                    }
                    else
                    {
                        go = new GameObject();
                        go.layer = layer;
                        go.transform.SetParent(parent, false);
                        go.transform.localPosition = CellToLocal(x, y);
                        go.transform.localRotation = rot;
                        go.transform.localScale = new Vector3(tileSize, 1f, tileSize);
                        go.AddComponent<MeshFilter>().sharedMesh = GetQuad();
                        mr = go.AddComponent<MeshRenderer>();
                        var mats = GetMats(styleId);
                        mr.sharedMaterial = mats[Hash(x, y, 2) % mats.Length];
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        if (floorColliders)
                        {
                            var bc = go.AddComponent<BoxCollider>();
                            bc.center = new Vector3(0f, -0.025f, 0f);
                            bc.size = new Vector3(1f, 0.05f, 1f);
                        }
                    }
                    go.name = "Cell_" + x + "_" + y;

                    var cv = go.AddComponent<CellView>();
                    cv.Init(x, y, zone, Model.KindAt(x, y), mr);
                    cells[x, y] = cv;
                }
        }

        // ================== Utilitaires ==================
        static int Hash(int x, int y, int salt)
        {
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(salt * 83492791);
                h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
                return (int)(h & 0x7fffffff);
            }
        }

        static void SetLayerRecursive(GameObject go, int l)
        {
            go.layer = l;
            foreach (Transform c in go.transform) SetLayerRecursive(c.gameObject, l);
        }

        static Mesh GetQuad()
        {
            if (quadMesh != null) return quadMesh;
            quadMesh = new Mesh { name = "FloorQuad", hideFlags = HideFlags.HideAndDontSave };
            quadMesh.vertices = new[] { new Vector3(-.5f, 0, -.5f), new Vector3(-.5f, 0, .5f), new Vector3(.5f, 0, .5f), new Vector3(.5f, 0, -.5f) };
            quadMesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            quadMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            quadMesh.RecalculateNormals(); quadMesh.RecalculateBounds();
            return quadMesh;
        }
    }
}