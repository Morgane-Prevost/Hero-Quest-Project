using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>
    /// Construit le contenu d'un niveau par-dessus la grid map. Pour l'instant : les murs.
    /// Ajoute ce composant pour voir le niveau ; sans lui, seule la grid map est affichée.
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        [Header("Références")]
        public BoardBuilder board;

        [Header("Niveau (JSON)")]
        [Tooltip("Niveau du projet (Assets/Script/Board/Levels). Prioritaire.")]
        public TextAsset levelJson;
        [Tooltip("Chemin relatif à StreamingAssets : utilisé seulement si Level Json est vide (extensions).")]
        public string levelPath = "";
        [Tooltip("Construit automatiquement dès que la grid map est prête.")]
        public bool autoBuild = true;

        [Header("Murs")]
        public float wallHeight = 1f;
        public float wallThickness = 0.12f;
        public bool wallColliders = false;
        public Material wallMaterial;                  // mur par défaut (cube) uniquement
        [Tooltip("Prefab de mur importé (n'importe quelle taille/pivot : adapté via ses Bounds).")]
        public GameObject wallPrefab;
        public PrefabFitMode wallFitMode = PrefabFitMode.Stretch;
        [Tooltip("Le mur dépasse d'une demi-épaisseur à chaque bout : coins sans trou.")]
        public bool wallOvershoot = true;

        // ---- API publique ----
        public LevelModel Model { get; private set; }
        public LevelData Data { get; private set; }
        public event Action<LevelBuilder> LevelBuilt;
        public event Action<EdgeKey, bool> EdgeOpenChanged;

        const string RootName = "LevelGenerated";

        readonly Dictionary<EdgeKey, GameObject> walls = new Dictionary<EdgeKey, GameObject>();
        readonly HashSet<EdgeKey> openEdges = new HashSet<EdgeKey>();
        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        Material runtimeWallMat;
        static Mesh cubeMesh;

        void Reset() { board = GetComponent<BoardBuilder>(); }

        void Awake() { if (board == null) board = GetComponent<BoardBuilder>(); }

        void OnEnable()
        {
            if (board == null) { Debug.LogError("[Level] Aucun BoardBuilder assigné."); return; }
            board.BoardBuilt += OnBoardBuilt;
            board.BoardCleared += OnBoardCleared;
        }

        void OnDisable()
        {
            if (board == null) return;
            board.BoardBuilt -= OnBoardBuilt;
            board.BoardCleared -= OnBoardCleared;
        }

        void Start()
        {
            // Cas où la grid map existe déjà au démarrage (construite manuellement avant)
            if (autoBuild && Model == null && board != null && board.Model != null) Build();
        }

        void OnDestroy() { DestroyOwned(); }

        void OnBoardBuilt(BoardBuilder b) { if (autoBuild) Build(); }
        void OnBoardCleared() { Clear(); }

        // ================== API murs ==================
        public GameObject GetWall(EdgeKey e) { GameObject g; return walls.TryGetValue(e, out g) ? g : null; }
        public bool IsEdgeOpen(EdgeKey e) { return openEdges.Contains(e); }

        /// <summary>
        /// Masque (open=true) ou rétablit un segment de mur. Utilisé par les portes (étape 2) :
        /// la porte remplace visuellement le mur. L'état logique reste à la charge du Lead Dev.
        /// </summary>
        public void SetEdgeOpen(EdgeKey e, bool open)
        {
            GameObject w;
            if (!walls.TryGetValue(e, out w)) { Debug.LogWarning("[Level] Aucun mur sur l'arête " + e); return; }
            if (open) openEdges.Add(e); else openEdges.Remove(e);
            w.SetActive(!open);
            if (EdgeOpenChanged != null) EdgeOpenChanged(e, open);
        }

        // ================== Construction ==================
        [ContextMenu("Build Level")]
        public bool Build()
        {
            LevelData data; string err;
            bool loaded = levelJson != null
                ? LevelLoader.TryParse(levelJson.text, levelJson.name, out data, out err)
                : LevelLoader.TryLoad(levelPath, out data, out err);
            if (!loaded) { Debug.LogError("[Level] " + err); return false; }
            return BuildFromData(data);
        }

        /// <summary>Pour le chargeur d'extensions / sélecteur de niveau.</summary>
        public bool BuildFromJson(string json, string sourceName = "extension")
        {
            LevelData data; string err;
            if (!LevelLoader.TryParse(json, sourceName, out data, out err)) { Debug.LogError("[Level] " + err); return false; }
            return BuildFromData(data);
        }

        public bool BuildFromData(LevelData data)
        {
            if (board == null || board.Model == null)
            { Debug.LogError("[Level] Construis d'abord la grid map (BoardBuilder)."); return false; }

            Clear();

            var errors = new List<string>(); var warnings = new List<string>();
            LevelModel model;
            bool ok = LevelLoader.TryBuildModel(data, board.Model, out model, errors, warnings);
            foreach (var w in warnings) Debug.LogWarning("[Level] " + w);
            if (!ok) { foreach (var e in errors) Debug.LogError("[Level] " + e); return false; }

            Data = data; Model = model;

            var root = new GameObject(RootName).transform;
            root.SetParent(board.transform, false); // mêmes coordonnées locales que la grid map
            root.gameObject.layer = board.BoardLayer;

            BuildWalls(root);

            Debug.Log("[Level] '" + Model.id + "' OK : " + Model.WallEdges.Count + " murs.");
            if (LevelBuilt != null) LevelBuilt(this);
            return true;
        }

        [ContextMenu("Clear Level")]
        public void Clear()
        {
            if (board != null)
            {
                var t = board.transform.Find(RootName);
                if (t != null) { t.name = RootName + "_old"; SafeDestroy(t.gameObject); }
            }
            DestroyOwned();
            walls.Clear(); openEdges.Clear();
            runtimeWallMat = null;
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

        // ---------- Murs ----------
        void BuildWalls(Transform parent)
        {
            int layer = board.BoardLayer;
            float len = board.tileSize + (wallOvershoot ? wallThickness : 0f);

            foreach (var e in Model.WallEdges)
            {
                GameObject w;
                Vector3 pos = board.EdgeToLocal(e);
                if (wallPrefab != null)
                {
                    w = PrefabFit.Instantiate(wallPrefab, parent, new Vector3(len, wallHeight, wallThickness), wallFitMode);
                }
                else
                {
                    w = CreateDefaultWall(parent);
                    w.transform.localScale = new Vector3(len, wallHeight, wallThickness);
                    pos.y = wallHeight * 0.5f;
                }
                w.name = "Wall_" + e;
                w.transform.localPosition = pos;
                w.transform.localRotation = e.horizontal ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
                SetLayerRecursive(w, layer);
                foreach (var c in w.GetComponentsInChildren<Collider>()) c.enabled = wallColliders;
                walls[e] = w;
            }
        }

        GameObject CreateDefaultWall(Transform parent)
        {
            if (runtimeWallMat == null)
            {
                if (wallMaterial != null) runtimeWallMat = wallMaterial;
                else
                {
                    runtimeWallMat = new Material(FloorStyleFactory.FindLitShader());
                    runtimeWallMat.color = new Color(0.93f, 0.91f, 0.85f); // murs blancs HeroQuest
                    owned.Add(runtimeWallMat);
                }
            }
            var go = new GameObject("Wall");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = GetCube();
            go.AddComponent<MeshRenderer>().sharedMaterial = runtimeWallMat;
            if (wallColliders) go.AddComponent<BoxCollider>();
            return go;
        }

        // ---------- Outil d'équipe ----------
        /// <summary>
        /// Copie dans le presse-papier le bloc "walls" en mode explicit contenant TOUS les murs actuels.
        /// Point de départ pratique pour écrire un niveau : on colle, puis on supprime les lignes inutiles.
        /// </summary>
        [ContextMenu("Copy Walls As Explicit JSON")]
        void CopyWallsAsExplicitJson()
        {
            if (Model == null || board == null || board.Model == null) { Debug.LogWarning("[Level] Rien à exporter."); return; }
            var sb = new StringBuilder();
            sb.AppendLine("\"walls\": {");
            sb.AppendLine("  \"mode\": \"explicit\",");
            sb.AppendLine("  \"addEdges\": [");
            int n = Model.WallEdges.Count;
            for (int i = 0; i < n; i++)
            {
                int cx, cy; char side;
                ToCellSide(Model.WallEdges[i], board.Model, out cx, out cy, out side);
                sb.Append("    { \"x\": ").Append(cx).Append(", \"y\": ").Append(cy)
                  .Append(", \"side\": \"").Append(side).Append("\" }").AppendLine(i < n - 1 ? "," : "");
            }
            sb.AppendLine("  ],");
            sb.AppendLine("  \"removeEdges\": []");
            sb.Append("}");
            GUIUtility.systemCopyBuffer = sb.ToString();
            Debug.Log("[Level] " + n + " murs copiés dans le presse-papier (mode explicit).");
        }

        /// <summary>Décrit l'arête depuis une case du plateau (côté S/W si possible, sinon N/E pour le pourtour).</summary>
        static void ToCellSide(EdgeKey e, BoardModel b, out int cx, out int cy, out char side)
        {
            var cb = e.CellB;
            if (b.InBounds(cb.x, cb.y)) { cx = cb.x; cy = cb.y; side = e.horizontal ? 'S' : 'W'; }
            else { var ca = e.CellA; cx = ca.x; cy = ca.y; side = e.horizontal ? 'N' : 'E'; }
        }

        // ---------- Utilitaires ----------
        static void SetLayerRecursive(GameObject go, int l)
        {
            go.layer = l;
            foreach (Transform c in go.transform) SetLayerRecursive(c.gameObject, l);
        }

        static Mesh GetCube()
        {
            if (cubeMesh != null) return cubeMesh;
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Destroy(tmp); else DestroyImmediate(tmp);
            return cubeMesh;
        }
    }
}