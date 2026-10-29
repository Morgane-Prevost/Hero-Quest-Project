using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>Replaces the visual of an element type (e.g., "Chest", "Door", "Goblin", "Pit") with an imported prefab.</summary>
    [Serializable]
    public class PlaceableBinding
    {
        public string type;
        [Tooltip("Any pivot. Model front face = +Z.")]
        public GameObject prefab;
        public PrefabFitMode fit = PrefabFitMode.Stretch;
        [Tooltip("Desired height (0 = model's original height).")]
        public float height = 0f;
    }

    /// <summary>
    /// Builds the contents of a level on top of the grid map: walls, doors, stairs, obstacles,
    /// furniture/chests, monsters, traps. Also handles revelation (Heroes screen vs Zargon screen).
    /// </summary>
    public class LevelBuilder : MonoBehaviour, ISpellDoorTarget
    {
        [Header("References")]
        public BoardBuilder board;

        [Header("Level (JSON)")]
        public TextAsset levelJson;
        public string levelPath = "";
        public bool autoBuild = true;

        [Header("Walls")]
        public float wallHeight = 1f;
        public float wallThickness = 0.12f;
        public bool wallColliders = true;
        public Material wallMaterial;
        public GameObject wallPrefab;
        public PrefabFitMode wallFitMode = PrefabFitMode.Stretch;
        public bool wallOvershoot = true;

        [Header("Visibility Layers")]
        [Tooltip("Layer visible ONLY by Zargon's screen (elements not yet revealed to heroes).")]
        public string zargonLayerName = "ZargonView";

        [Header("Elements: Imported Visuals (replaces placeholders)")]
        public List<PlaceableBinding> placeables = new List<PlaceableBinding>();

        // ---- Public API ----
        public LevelModel Model { get; private set; }
        public LevelData Data { get; private set; }
        public IEnumerable<PlacedElement> Elements { get { return placed.Values; } }

        public event Action<LevelBuilder> LevelBuilt;
        /// <summary>A wall segment has been visually hidden/restored (revealed door...).</summary>
        public event Action<EdgeKey, bool> EdgeOpenChanged;
        public event Action<ElementDef> ElementRevealed;             // became visible to heroes
        public event Action<ElementDef, bool> ElementOpened;         // door / chest opened
        public event Action<ElementDef, TrapState> TrapStateChanged;

        const string RootName = "LevelGenerated";

        readonly Dictionary<EdgeKey, GameObject> walls = new Dictionary<EdgeKey, GameObject>();
        readonly HashSet<EdgeKey> openEdges = new HashSet<EdgeKey>();
        readonly Dictionary<string, PlacedElement> placed = new Dictionary<string, PlacedElement>();
        readonly HashSet<int> revealedZones = new HashSet<int>();
        readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        Material runtimeWallMat;
        int boardLayer, zargonLayer;
        static readonly Dictionary<PrimitiveType, Mesh> primMeshes = new Dictionary<PrimitiveType, Mesh>();

        void Reset() { board = GetComponent<BoardBuilder>(); }
        void Awake() { if (board == null) board = GetComponent<BoardBuilder>(); }

        void OnEnable()
        {
            if (board == null) { Debug.LogError("[Level] No BoardBuilder assigned."); return; }
            board.BoardBuilt += OnBoardBuilt;
            board.BoardCleared += OnBoardCleared;
        }

        void OnDisable()
        {
            if (board == null) return;
            board.BoardBuilt -= OnBoardBuilt;
            board.BoardCleared -= OnBoardCleared;
        }

        void Start() { if (autoBuild && Model == null && board != null && board.Model != null) Build(); }
        void OnDestroy() { DestroyOwned(); }
        void OnBoardBuilt(BoardBuilder b) { if (autoBuild) Build(); }
        void OnBoardCleared() { Clear(); }

        // =====================================================================
        //  Walls
        // =====================================================================
        public GameObject GetWall(EdgeKey e) { GameObject g; return walls.TryGetValue(e, out g) ? g : null; }
        public bool IsEdgeOpen(EdgeKey e) { return openEdges.Contains(e); }

        /// <summary>Hides (open=true) or visually restores a wall. Doors already use this automatically.</summary>
        public void SetEdgeOpen(EdgeKey e, bool open)
        {
            if (!walls.ContainsKey(e)) { Debug.LogWarning("[Level] No wall on edge " + e); return; }
            HideWall(e, open);
            if (EdgeOpenChanged != null) EdgeOpenChanged(e, open);
        }

        void HideWall(EdgeKey e, bool hide)
        {
            GameObject w;
            if (!walls.TryGetValue(e, out w)) return;
            bool was = openEdges.Contains(e);
            if (hide) openEdges.Add(e); else openEdges.Remove(e);
            w.SetActive(!hide);
            if (was != hide && EdgeOpenChanged != null) EdgeOpenChanged(e, hide);
        }

        // =====================================================================
        //  Revelation (Heroes screen): called by Lead Dev
        // =====================================================================
        public PlacedElement GetElement(string id) { PlacedElement p; return placed.TryGetValue(id, out p) ? p : null; }
        public bool IsZoneRevealed(int zone) { return revealedZones.Contains(zone); }

        /// <summary>Reveals a specific element to the heroes (secret door found, hidden treasure found, monster in line of sight...).</summary>
        public bool RevealElement(string id)
        {
            PlacedElement pe;
            if (!TryGet(id, out pe)) return false;
            if (pe.Def.kind == ElementKind.Trap) return HighlightTrap(id);
            return Reveal(pe);
        }

        /// <summary>Reveals all NON-hidden contents of a zone (room N). Zone 0 = all corridors (to be avoided).</summary>
        public void RevealZone(int zone)
        {
            revealedZones.Add(zone);
            foreach (var pe in placed.Values)
                if (!pe.Def.hidden && pe.Def.kind != ElementKind.Trap && pe.Def.zones.Contains(zone)) Reveal(pe);
        }

        /// <summary>Reveals the non-hidden contents of a cell (line of sight in a corridor).</summary>
        public void RevealCell(int x, int y)
        {
            foreach (var pe in placed.Values)
                if (!pe.Def.hidden && pe.Def.kind != ElementKind.Trap && pe.Def.Covers(x, y)) Reveal(pe);
        }

        bool Reveal(PlacedElement pe)
        {
            if (pe.VisibleToHeroes) return false;
            pe.VisibleToHeroes = true;
            ApplyVisibility(pe);
            if (ElementRevealed != null) ElementRevealed(pe.Def);
            return true;
        }

        /// <summary>Opens a door (irreversible): reveals it and reveals the rooms on both sides.</summary>
        public bool OpenDoor(string id)
        {
            PlacedElement pe;
            if (!TryGet(id, out pe)) return false;
            if (pe.Def.kind != ElementKind.Door) { Debug.LogWarning("[Level] '" + id + "' is not a door."); return false; }
            Reveal(pe);
            bool changed = SetOpen(id, true);
            foreach (int z in pe.Def.zones) if (z > 0) RevealZone(z);
            return changed;
        }

        /// <summary>Opens / closes a door or chest (visual: Animator "Open" parameter if present, otherwise placeholder squashed).</summary>
        public bool SetOpen(string id, bool open)
        {
            PlacedElement pe;
            if (!TryGet(id, out pe)) return false;
            ElementKind k = pe.Def.kind;
            if (k != ElementKind.Door && k != ElementKind.Furniture) { Debug.LogWarning("[Level] '" + id + "' cannot be opened."); return false; }
            if (pe.IsOpen == open) return false;
            if (!open && k == ElementKind.Door) { Debug.LogWarning("[Level] An open door cannot be closed."); return false; }

            pe.IsOpen = open;
            if (pe.animator != null) pe.animator.SetBool("Open", open);
            else if (pe.visual != null)
            {
                Vector3 s = pe.visual.transform.localScale;
                pe.visual.transform.localScale = new Vector3(s.x, open ? s.y * 0.2f : s.y * 5f, s.z);
            }
            if (ElementOpened != null) ElementOpened(pe.Def, open);
            return true;
        }

        /// <summary>A hero searched for traps: the cell becomes suspicious (outline, WITHOUT tile).</summary>
        public bool HighlightTrap(string id) { return SetTrap(id, TrapState.Highlighted); }

        /// <summary>The trap triggers: the tile is placed.</summary>
        public bool TriggerTrap(string id) { return SetTrap(id, TrapState.Triggered); }

        bool SetTrap(string id, TrapState s)
        {
            PlacedElement pe;
            if (!TryGet(id, out pe)) return false;
            if (pe.Def.kind != ElementKind.Trap) { Debug.LogWarning("[Level] '" + id + "' is not a trap."); return false; }
            if (s <= pe.Trap) return false;
            pe.Trap = s;
            ApplyVisibility(pe);
            if (TrapStateChanged != null) TrapStateChanged(pe.Def, s);
            return true;
        }

        bool TryGet(string id, out PlacedElement pe)
        {
            if (placed.TryGetValue(id, out pe)) return true;
            Debug.LogWarning("[Level] Unknown element: '" + id + "'.");
            return false;
        }

        // =====================================================================
        //  Construction
        // =====================================================================
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

        public bool BuildFromJson(string json, string sourceName = "extension")
        {
            LevelData data; string err;
            if (!LevelLoader.TryParse(json, sourceName, out data, out err)) { Debug.LogError("[Level] " + err); return false; }
            return BuildFromData(data);
        }

        public bool BuildFromData(LevelData data)
        {
            if (board == null || board.Model == null)
            { Debug.LogError("[Level] Build the grid map first (BoardBuilder)."); return false; }

            Clear();

            var errors = new List<string>(); var warnings = new List<string>();
            LevelModel model;
            bool ok = LevelLoader.TryBuildModel(data, board.Model, out model, errors, warnings);
            foreach (var w in warnings) Debug.LogWarning("[Level] " + w);
            if (!ok) { foreach (var e in errors) Debug.LogError("[Level] " + e); return false; }

            Data = data; Model = model;
            ResolveLayers();

            var root = new GameObject(RootName).transform;
            root.SetParent(board.transform, false);
            root.gameObject.layer = boardLayer;

            BuildWalls(root);
            BuildElements(root);
            ApplyInitialReveal();

            Debug.Log("[Level] '" + Model.id + "' OK: " + Model.WallEdges.Count + " walls, " + Model.Elements.Count + " elements.");
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
            walls.Clear(); openEdges.Clear(); placed.Clear(); revealedZones.Clear(); mats.Clear();
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

        void ResolveLayers()
        {
            boardLayer = board.BoardLayer;
            zargonLayer = LayerMask.NameToLayer(zargonLayerName);
            if (zargonLayer < 0)
            {
                zargonLayer = boardLayer;
                Debug.LogWarning("[Level] Layer '" + zargonLayerName + "' not found: hidden elements will be visible to ALL. " +
                                 "Create it in Project Settings > Tags and Layers.");
            }
        }

        // ---------- Walls ----------
        void BuildWalls(Transform parent)
        {
            float len = board.tileSize + (wallOvershoot ? wallThickness : 0f);
            foreach (var e in Model.WallEdges)
            {
                GameObject w;
                Vector3 pos = board.EdgeToLocal(e);
                if (wallPrefab != null)
                    w = PrefabFit.Instantiate(wallPrefab, parent, new Vector3(len, wallHeight, wallThickness), wallFitMode);
                else
                {
                    w = CreateDefaultWall(parent);
                    w.transform.localScale = new Vector3(len, wallHeight, wallThickness);
                    pos.y = wallHeight * 0.5f;
                }
                w.name = "Wall_" + e;
                w.transform.localPosition = pos;
                w.transform.localRotation = e.horizontal ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
                SetLayerRecursive(w, boardLayer);
                w.layer = LayerMask.NameToLayer("Obstacle");
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
                    runtimeWallMat.color = new Color(0.93f, 0.91f, 0.85f); // HeroQuest white walls
                    owned.Add(runtimeWallMat);
                }
            }
            var go = new GameObject("Wall");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = GetPrimMesh(PrimitiveType.Cube);
            go.AddComponent<MeshRenderer>().sharedMaterial = runtimeWallMat;
            if (wallColliders) go.AddComponent<BoxCollider>();
            return go;
        }

        // ---------- Elements ----------
        void BuildElements(Transform parent)
        {
            var holder = new GameObject("Elements").transform;
            holder.SetParent(parent, false);
            holder.gameObject.layer = boardLayer;

            foreach (var def in Model.Elements)
            {
                var go = new GameObject(def.kind + "_" + def.id);
                go.transform.SetParent(holder, false);
                var pe = go.AddComponent<PlacedElement>();
                pe.Init(def);

                switch (def.kind)
                {
                    case ElementKind.Door: BuildDoor(pe); break;
                    case ElementKind.Trap: BuildTrap(pe); break;
                    case ElementKind.Monster: BuildMonster(pe); break;
                    default: BuildSolid(pe); break;     // Stairs, Blocked, Furniture
                }
                placed[def.id] = pe;
            }
        }

        static float Yaw(Dir d)
        {
            switch (d) { case Dir.E: return 90f; case Dir.S: return 180f; case Dir.W: return 270f; default: return 0f; }
        }

        static bool IsChest(string type)
        {
            return type != null && type.IndexOf("chest", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void BuildSolid(PlacedElement pe)
        {
            ElementDef d = pe.Def; float t = board.tileSize;
            pe.transform.localPosition = new Vector3((d.x + d.w * 0.5f) * t, 0f, (d.y + d.h * 0.5f) * t);
            pe.transform.localRotation = Quaternion.Euler(0f, Yaw(d.facing), 0f);

            // The model is built "facing +Z" then rotated: for E/W, width and depth are swapped.
            bool swap = d.facing == Dir.E || d.facing == Dir.W;
            float sx = (swap ? d.h : d.w) * t * 0.94f, sz = (swap ? d.w : d.h) * t * 0.94f;

            float height; Color color; bool nub = false;
            if (d.kind == ElementKind.Stairs) { height = 0.05f; color = new Color(0.35f, 0.45f, 0.75f); }
            else if (d.kind == ElementKind.Blocked) { height = 0.8f; color = new Color(0.25f, 0.25f, 0.27f); }
            else
            {
                bool chest = IsChest(d.type);
                height = chest ? 0.3f : 0.5f;
                color = chest ? new Color(0.9f, 0.7f, 0.2f) : new Color(0.6f, 0.55f, 0.5f);
                nub = true;                                     // small "front face" marker
            }
            pe.visual = CreateVisual(d.type, pe.transform, new Vector3(sx, height, sz), color, PrimitiveType.Cube, nub, 0f);
        }

        void BuildDoor(PlacedElement pe)
        {
            ElementDef d = pe.Def;
            pe.transform.localPosition = board.EdgeToLocal(d.edge);
            pe.transform.localRotation = d.edge.horizontal ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
            // Slightly thicker than the wall: visible over the wall until hidden (Zargon view).
            Vector3 size = new Vector3(board.tileSize * 0.98f, wallHeight * 0.95f, wallThickness * 1.5f);
            pe.visual = CreateVisual(d.type, pe.transform, size, new Color(0.45f, 0.28f, 0.12f), PrimitiveType.Cube, false, size.y);
            pe.animator = pe.visual.GetComponentInChildren<Animator>();
        }

        void BuildMonster(PlacedElement pe)
        {
            ElementDef d = pe.Def; float t = board.tileSize;
            var pawn = pe.gameObject.AddComponent<GridPawn>();
            pawn.board = board;
            pawn.PlaceAt(d.x, d.y);                             // at the center of the cell
            pe.transform.rotation = board.transform.rotation * Quaternion.Euler(0f, Yaw(d.facing), 0f);
            pe.Pawn = pawn;
            pe.visual = CreateVisual(d.type, pe.transform, new Vector3(0.7f * t, 0.6f * t, 0.7f * t),
                                   new Color(0.75f, 0.15f, 0.15f), PrimitiveType.Cylinder, true, 0f);
        }

        void BuildTrap(PlacedElement pe)
        {
            ElementDef d = pe.Def; float t = board.tileSize;
            pe.transform.localPosition = board.CellToLocal(d.x, d.y);
            pe.marker = CreateVisual("TrapMarker", pe.transform, new Vector3(0.5f * t, 0.03f, 0.5f * t), new Color(1f, 0.8f, 0.1f), PrimitiveType.Cube, false, 0f);
            pe.highlight = CreateVisual("TrapHighlight", pe.transform, new Vector3(0.92f * t, 0.02f, 0.92f * t), new Color(1f, 1f, 0.4f), PrimitiveType.Cylinder, false, 0f);
            pe.tile = CreateVisual(d.type, pe.transform, new Vector3(0.94f * t, 0.04f, 0.94f * t), new Color(0.1f, 0.08f, 0.08f), PrimitiveType.Cube, false, 0f);
        }

        // ---------- Visibility ----------
        void ApplyInitialReveal()
        {
            foreach (var pe in placed.Values)
            {
                pe.VisibleToHeroes = pe.Def.kind == ElementKind.Stairs;   // stairs are always visible
                pe.Trap = TrapState.Hidden;
                ApplyVisibility(pe);
            }
            foreach (int z in Model.InitialZones) RevealZone(z);
        }

        /// <summary>
        /// Shared layer (Board) = visible by both screens. ZargonView = visible by Zargon only.
        /// A revealed door visually replaces its wall.
        /// </summary>
        void ApplyVisibility(PlacedElement pe)
        {
            if (pe.Def.kind == ElementKind.Trap)
            {
                bool triggered = pe.Trap == TrapState.Triggered;
                SetActiveAndLayer(pe.marker, !triggered, zargonLayer);                    // gold marker: Zargon only
                SetActiveAndLayer(pe.highlight, pe.Trap == TrapState.Highlighted, boardLayer);
                SetActiveAndLayer(pe.tile, triggered, boardLayer);
                return;
            }
            SetLayerRecursive(pe.gameObject, pe.VisibleToHeroes ? boardLayer : zargonLayer);
            if (pe.Def.kind == ElementKind.Door) HideWall(pe.Def.edge, pe.VisibleToHeroes);
        }

        static void SetActiveAndLayer(GameObject g, bool active, int layer)
        {
            if (g == null) return;
            g.SetActive(active);
            if (active) SetLayerRecursive(g, layer);
        }

        // ---------- Visuals (imported prefab or placeholder) ----------
        PlaceableBinding FindBinding(string type)
        {
            if (string.IsNullOrEmpty(type)) return null;
            foreach (var b in placeables)
                if (b != null && b.prefab != null && string.Equals(b.type, type, StringComparison.OrdinalIgnoreCase)) return b;
            return null;
        }

        GameObject CreateVisual(string type, Transform parent, Vector3 size, Color color, PrimitiveType prim,
                                bool nub, float prefabHeightFallback)
        {
            PlaceableBinding b = FindBinding(type);
            if (b != null)
            {
                float h = b.height > 0f ? b.height : prefabHeightFallback;
                return PrefabFit.Instantiate(b.prefab, parent, new Vector3(size.x, h, size.z), b.fit);
            }
            return MakePlaceholder("Placeholder_" + type, parent, size, color, prim, nub);
        }

        GameObject MakePlaceholder(string name, Transform parent, Vector3 size, Color color, PrimitiveType prim, bool nub)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);

            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            float meshH = prim == PrimitiveType.Cylinder ? 2f : 1f;
            body.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            body.transform.localScale = new Vector3(size.x, size.y / meshH, size.z);
            body.AddComponent<MeshFilter>().sharedMesh = GetPrimMesh(prim);
            body.AddComponent<MeshRenderer>().sharedMaterial = GetMat(color);

            if (nub)
            {
                var n = new GameObject("FrontMark");
                n.transform.SetParent(root.transform, false);
                n.transform.localPosition = new Vector3(0f, size.y * 0.5f, size.z * 0.5f);
                n.transform.localScale = new Vector3(Mathf.Min(0.2f, size.x * 0.4f), Mathf.Min(0.2f, size.y * 0.5f), 0.12f);
                n.AddComponent<MeshFilter>().sharedMesh = GetPrimMesh(PrimitiveType.Cube);
                n.AddComponent<MeshRenderer>().sharedMaterial = GetMat(new Color(0.95f, 0.95f, 0.9f));
            }
            return root;
        }

        Material GetMat(Color c)
        {
            string key = ColorUtility.ToHtmlStringRGB(c);
            Material m;
            if (!mats.TryGetValue(key, out m))
            {
                m = new Material(FloorStyleFactory.FindLitShader());
                m.color = c; m.name = "LevelMat_" + key;
                mats[key] = m; owned.Add(m);
            }
            return m;
        }

        // ---------- Team Tool ----------
        /// <summary>Copies the "walls" block in explicit mode containing ALL current walls to the clipboard.</summary>
        [ContextMenu("Copy Walls As Explicit JSON")]
        void CopyWallsAsExplicitJson()
        {
            if (Model == null || board == null || board.Model == null) { Debug.LogWarning("[Level] Nothing to export."); return; }
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
            Debug.Log("[Level] " + n + " walls copied to clipboard (explicit mode).");
        }

        static void ToCellSide(EdgeKey e, BoardModel b, out int cx, out int cy, out char side)
        {
            var cb = e.CellB;
            if (b.InBounds(cb.x, cb.y)) { cx = cb.x; cy = cb.y; side = e.horizontal ? 'S' : 'W'; }
            else { var ca = e.CellA; cx = ca.x; cy = ca.y; side = e.horizontal ? 'N' : 'E'; }
        }

        // ---------- Utilities ----------
        static void SetLayerRecursive(GameObject go, int l)
        {
            go.layer = l;
            foreach (Transform c in go.transform) SetLayerRecursive(c.gameObject, l);
        }

        static Mesh GetPrimMesh(PrimitiveType type)
        {
            Mesh m;
            if (primMeshes.TryGetValue(type, out m) && m != null) return m;
            var tmp = GameObject.CreatePrimitive(type);
            m = tmp.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Destroy(tmp); else DestroyImmediate(tmp);
            primMeshes[type] = m;
            return m;
        }
    }
}