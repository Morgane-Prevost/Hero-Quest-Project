using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HeroQuest.Board
{
    /// <summary>Which side of a room/corridor wall receives the textured face of a one-sided wall prefab.</summary>
    public enum WallFaceRule { TowardRoom, TowardCorridor, BothSides }

    /// <summary>How the wall prefab is textured.</summary>
    public enum WallPrefabFaces
    {
        OneSidePlusZ,   // textured on its +Z face, flat on -Z (the builder orients it, or doubles it back to back)
        BothSides       // textured on both faces: used as is, one instance per wall
    }

    /// <summary>Replaces the visual of an element type (e.g. "Chest", "Door", "Goblin", "Pit") with imported prefabs.</summary>
    [Serializable]
    public class PlaceableBinding
    {
        public string type;
        [Tooltip("Closed / default model. Any pivot. The model's front face must point to +Z.")]
        public GameObject prefab;
        [Tooltip("Optional model shown once the element is opened (open chest, open door). Same conventions as Prefab.")]
        public GameObject openPrefab;
        public PrefabFitMode fit = PrefabFitMode.Stretch;
        [Tooltip("Wanted height (0 = keep the model's native height).")]
        public float height = 0f;
    }

    /// <summary>
    /// Builds the content of one level on top of the grid map: walls, doors, stairs, blocked squares,
    /// furniture/chests, monsters, traps. Also handles visibility (heroes screen vs Zargon screen),
    /// door/chest opening, trap states and unused rooms.
    /// </summary>
    public class LevelBuilder : MonoBehaviour, ISpellDoorTarget
    {
        [Header("References")]
        public BoardBuilder board;

        [Header("Level (JSON)")]
        [Tooltip("Level file of the project (Assets/Script/Board/Levels). Has priority.")]
        public TextAsset levelJson;
        [Tooltip("Path relative to StreamingAssets, used only when Level Json is empty (extensions).")]
        public string levelPath = "";
        [Tooltip("Build automatically as soon as the grid map is ready.")]
        public bool autoBuild = true;

        [Header("Walls")]
        public float wallHeight = 1f;
        public float wallThickness = 0.12f;
        public bool wallColliders = false;
        [Tooltip("Used by the default wall (cube) only.")]
        public bool wallColliders = true;
        public Material wallMaterial;
        [Tooltip("Imported wall prefab (any size/pivot: fitted through its Bounds). Textured face = +Z.")]
        public GameObject wallPrefab;
        public PrefabFitMode wallFitMode = PrefabFitMode.Stretch;
        [Tooltip("OneSidePlusZ: textured on +Z only. BothSides: textured on both faces.")]
        public WallPrefabFaces wallFaces = WallPrefabFaces.OneSidePlusZ;
        [Tooltip("Room/corridor walls: side that gets the textured face (one-sided prefab only).")]
        public WallFaceRule wallTexturedFace = WallFaceRule.TowardRoom;
        [Tooltip("Walls overshoot by half a thickness at both ends so corners have no gap.")]
        public bool wallOvershoot = true;

        [Header("Visibility layers")]
        [Tooltip("Layer seen ONLY by Zargon's screen (elements not yet revealed to the heroes).")]
        public string zargonLayerName = "ZargonView";

        [Header("Elements: imported visuals (replace the placeholders)")]
        public List<PlaceableBinding> placeables = new List<PlaceableBinding>();

        // ---- Public API ----
        public LevelModel Model { get; private set; }
        public LevelData Data { get; private set; }
        public IEnumerable<PlacedElement> Elements { get { return placed.Values; } }

        public event Action<LevelBuilder> LevelBuilt;
        /// <summary>A wall segment was hidden/restored visually (a door was revealed, ...).</summary>
        public event Action<EdgeKey, bool> EdgeOpenChanged;
        /// <summary>An element became visible to the heroes.</summary>
        public event Action<ElementDef> ElementRevealed;
        /// <summary>A door or chest was opened/closed.</summary>
        public event Action<ElementDef, bool> ElementOpened;
        public event Action<ElementDef, TrapState> TrapStateChanged;

        const string RootName = "LevelGenerated";

        // Which wall faces must be textured for a given edge.
        enum WallFace { TowardA, TowardB, Both }   // A = South/West cell, B = North/East cell

        // Size used to build an element's visual (needed to build its "open" model later).
        sealed class VisualInfo
        {
            public Vector3 size;
            public float heightFallback;
            public GameObject openVisual;
        }

        readonly Dictionary<EdgeKey, GameObject> walls = new Dictionary<EdgeKey, GameObject>();
        readonly HashSet<EdgeKey> openEdges = new HashSet<EdgeKey>();
        readonly Dictionary<string, PlacedElement> placed = new Dictionary<string, PlacedElement>();
        readonly Dictionary<string, VisualInfo> visuals = new Dictionary<string, VisualInfo>();
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
            // Covers the case where the grid map was already built before this component started.
            if (autoBuild && Model == null && board != null && board.Model != null) Build();
        }

        void OnDestroy() { DestroyOwned(); }
        void OnBoardBuilt(BoardBuilder b) { if (autoBuild) Build(); }
        void OnBoardCleared() { Clear(); }

        // =====================================================================
        //  Walls
        // =====================================================================
        public GameObject GetWall(EdgeKey e) { GameObject g; return walls.TryGetValue(e, out g) ? g : null; }
        public bool IsEdgeOpen(EdgeKey e) { return openEdges.Contains(e); }

        /// <summary>Hides (open=true) or restores a wall segment visually. Doors already do this on their own.</summary>
        public void SetEdgeOpen(EdgeKey e, bool open)
        {
            if (!walls.ContainsKey(e)) { Debug.LogWarning("[Level] Aucun mur sur l'arête " + e); return; }
            HideWall(e, open);
        }

        // Hides/restores a wall and raises EdgeOpenChanged only when the state really changes.
        void HideWall(EdgeKey e, bool hide)
        {
            GameObject w;
            if (!walls.TryGetValue(e, out w)) return;
            bool was = openEdges.Contains(e);
            if (was == hide) return;
            if (hide) openEdges.Add(e); else openEdges.Remove(e);
            w.SetActive(!hide);
            if (EdgeOpenChanged != null) EdgeOpenChanged(e, hide);
        }

        // =====================================================================
        //  Reveal (heroes screen): called by the game logic
        // =====================================================================
        public PlacedElement GetElement(string id) { PlacedElement p; return placed.TryGetValue(id, out p) ? p : null; }
        public bool IsZoneRevealed(int zone) { return revealedZones.Contains(zone); }

        /// <summary>Reveals one specific element to the heroes (secret door found, hidden treasure found, monster in sight...).</summary>
        public bool RevealElement(string id)
        {
            PlacedElement pe;
            if (!TryGet(id, out pe)) return false;
            if (pe.Def.kind == ElementKind.Trap) return HighlightTrap(id);
            return Reveal(pe);
        }

        /// <summary>Reveals every NON hidden element of a zone (room N). Zone 0 = all corridors (avoid).</summary>
        public void RevealZone(int zone)
        {
            revealedZones.Add(zone);
            foreach (var pe in placed.Values)
                if (!pe.Def.hidden && pe.Def.kind != ElementKind.Trap && pe.Def.zones.Contains(zone)) Reveal(pe);
        }

        /// <summary>Reveals the non hidden content of one cell (line of sight in a corridor).</summary>
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
            // Optional reveal animation: Animator trigger parameter named "Reveal".
            if (HasParam(pe.animator, "Reveal", AnimatorControllerParameterType.Trigger)) pe.animator.SetTrigger("Reveal");
            if (ElementRevealed != null) ElementRevealed(pe.Def);
            return true;
        }

        /// <summary>Opens a door (irreversible): reveals it and reveals the rooms on both sides.</summary>
        public bool OpenDoor(string id)
        {
            PlacedElement pe;
            if (!TryGet(id, out pe)) return false;
            if (pe.Def.kind != ElementKind.Door) { Debug.LogWarning("[Level] '" + id + "' n'est pas une porte."); return false; }
            Reveal(pe);
            bool changed = SetOpen(id, true);
            foreach (int z in pe.Def.zones) if (z > 0) RevealZone(z);
            return changed;
        }

        /// <summary>
        /// Opens/closes a door or a chest. Visual, by priority: the binding's Open Prefab, the "Open" bool of the
        /// prefab's Animator, otherwise the placeholder is squashed. A door never closes again.
        /// </summary>
        public bool SetOpen(string id, bool open)
        {
            PlacedElement pe;
            if (!TryGet(id, out pe)) return false;
            ElementKind k = pe.Def.kind;
            if (k != ElementKind.Door && k != ElementKind.Furniture) { Debug.LogWarning("[Level] '" + id + "' ne peut pas s'ouvrir."); return false; }
            if (pe.IsOpen == open) return false;
            if (!open && k == ElementKind.Door) { Debug.LogWarning("[Level] Une porte ouverte ne se referme pas."); return false; }

            pe.IsOpen = open;

            VisualInfo vi;
            PlaceableBinding b = FindBinding(pe.Def.type);
            if (b != null && b.openPrefab != null && pe.visual != null && visuals.TryGetValue(pe.Def.id, out vi))
            {
                // Swap the closed model for the open one (built on first use).
                if (vi.openVisual == null)
                {
                    float h = b.height > 0f ? b.height : vi.heightFallback;
                    vi.openVisual = PrefabFit.Instantiate(b.openPrefab, pe.transform, new Vector3(vi.size.x, h, vi.size.z), b.fit);
                }
                pe.visual.SetActive(!open);
                vi.openVisual.SetActive(open);
                ApplyVisibility(pe);   // gives the right layer to the model that was just created
            }
            else if (HasParam(pe.animator, "Open", AnimatorControllerParameterType.Bool))
            {
                pe.animator.SetBool("Open", open);
            }
            else if (pe.visual != null)
            {
                Vector3 s = pe.visual.transform.localScale;
                pe.visual.transform.localScale = new Vector3(s.x, open ? s.y * 0.2f : s.y * 5f, s.z);
            }
            if (ElementOpened != null) ElementOpened(pe.Def, open);
            return true;
        }

        /// <summary>A hero searched for traps: the cell becomes suspect (outline only, NO tile).</summary>
        public bool HighlightTrap(string id) { return SetTrap(id, TrapState.Highlighted); }

        /// <summary>The trap is triggered: its tile is laid down.</summary>
        public bool TriggerTrap(string id) { return SetTrap(id, TrapState.Triggered); }

        bool SetTrap(string id, TrapState s)
        {
            PlacedElement pe;
            if (!TryGet(id, out pe)) return false;
            if (pe.Def.kind != ElementKind.Trap) { Debug.LogWarning("[Level] '" + id + "' n'est pas un piège."); return false; }
            if (s <= pe.Trap) return false;                 // states only progress: Hidden < Highlighted < Triggered
            pe.Trap = s;
            ApplyVisibility(pe);
            if (TrapStateChanged != null) TrapStateChanged(pe.Def, s);
            return true;
        }

        bool TryGet(string id, out PlacedElement pe)
        {
            if (placed.TryGetValue(id, out pe)) return true;
            Debug.LogWarning("[Level] Élément inconnu : '" + id + "'.");
            return false;
        }

        // True when the animator has a parameter with this name and type (avoids Unity warnings).
        static bool HasParam(Animator a, string name, AnimatorControllerParameterType type)
        {
            if (a == null || a.runtimeAnimatorController == null) return false;
            foreach (var p in a.parameters) if (p.type == type && p.name == name) return true;
            return false;
        }

        // =====================================================================
        //  Build
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

        /// <summary>For the extension loader / level selector: build from a JSON string.</summary>
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
            ResolveLayers();
            ApplyUnusedRooms();

            var root = new GameObject(RootName).transform;
            root.SetParent(board.transform, false);   // same local coordinates as the grid map
            root.gameObject.layer = boardLayer;

            BuildWalls(root);
            BuildElements(root);
            ApplyInitialReveal();

            Debug.Log("[Level] '" + Model.id + "' OK : " + Model.WallEdges.Count + " murs, " +
                      Model.Elements.Count + " éléments, " + Model.UnusedZones.Count + " salles inutilisées.");
            if (LevelBuilt != null) LevelBuilt(this);
            return true;
        }

        [ContextMenu("Clear Level")]
        public void Clear()
        {
            RestoreAllCells();   // give back the floors hidden by the previous level

            if (board != null)
            {
                var t = board.transform.Find(RootName);
                if (t != null) { t.name = RootName + "_old"; SafeDestroy(t.gameObject); }
            }
            DestroyOwned();
            walls.Clear(); openEdges.Clear(); placed.Clear(); visuals.Clear(); revealedZones.Clear(); mats.Clear();
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
                Debug.LogWarning("[Level] Layer '" + zargonLayerName + "' introuvable : les éléments cachés seront visibles par TOUS. " +
                                 "Crée-le dans Project Settings > Tags and Layers.");
            }
        }

        // ---------- Unused rooms ----------
        // Hides the floor of every room the level declares as unused (cells then refuse pawns and picking).
        void ApplyUnusedRooms()
        {
            foreach (int zone in Model.UnusedZones)
                foreach (var c in board.Model.RoomCells(zone))
                {
                    CellView cv = board.GetCell(c.x, c.y);
                    if (cv != null) cv.SetPlayable(false);
                }
        }

        // Restores every cell hidden by a previous level (called on clear / rebuild).
        void RestoreAllCells()
        {
            if (board == null || board.Model == null) return;
            for (int x = 0; x < board.Model.width; x++)
                for (int y = 0; y < board.Model.height; y++)
                {
                    CellView cv = board.GetCell(x, y);
                    if (cv != null && !cv.Playable) cv.SetPlayable(true);
                }
        }

        // ---------- Walls ----------
        // One root object per wall edge. In the root's local space +Z points toward cell B (North / East)
        // and -Z toward cell A (South / West). Slabs sit on the floor (pivot = bottom center).
        void BuildWalls(Transform parent)
        {
            float len = board.tileSize + (wallOvershoot ? wallThickness : 0f);
            bool oneSided = wallPrefab != null && wallFaces == WallPrefabFaces.OneSidePlusZ;

            foreach (var e in Model.WallEdges)
            {
                var root = new GameObject("Wall_" + e);
                root.transform.SetParent(parent, false);
                root.transform.localPosition = board.EdgeToLocal(e);
                root.transform.localRotation = e.horizontal ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);

                if (!oneSided)
                {
                    AddWallSlab(root.transform, len, wallThickness, 0f, false);       // symmetric wall
                }
                else
                {
                    WallFace face = ChooseWallFace(e);
                    if (face == WallFace.TowardB) AddWallSlab(root.transform, len, wallThickness, 0f, false);
                    else if (face == WallFace.TowardA) AddWallSlab(root.transform, len, wallThickness, 0f, true);
                    else
                    {
                        // Both faces needed: two half-thickness slabs back to back (flat faces hidden inside).
                        float half = wallThickness * 0.5f;
                        AddWallSlab(root.transform, len, half, half * 0.5f, false);   // textured face toward B
                        AddWallSlab(root.transform, len, half, -half * 0.5f, true);   // textured face toward A
                    }
                }

                SetLayerRecursive(root, boardLayer);
                foreach (var c in root.GetComponentsInChildren<Collider>()) c.enabled = wallColliders;
                walls[e] = root;
            }
        }

        // Decides which side(s) of a wall must show the textured face.
        WallFace ChooseWallFace(EdgeKey e)
        {
            BoardModel bm = board.Model;
            Vector2Int a = e.CellA, b = e.CellB;
            int za = bm.ZoneAt(a.x, a.y), zb = bm.ZoneAt(b.x, b.y);
            bool pa = za >= 0 && !Model.IsUnusedZone(za);     // playable side?
            bool pb = zb >= 0 && !Model.IsUnusedZone(zb);

            if (pa && !pb) return WallFace.TowardA;           // next to void / unused room: face the playable side
            if (pb && !pa) return WallFace.TowardB;
            if (!pa && !pb) return WallFace.TowardB;          // nothing playable on either side (explicit walls only)

            if (wallTexturedFace == WallFaceRule.BothSides) return WallFace.Both;
            bool ra = za > 0, rb = zb > 0;                    // is it a room?
            if (ra == rb) return WallFace.Both;               // room|room or corridor|corridor
            bool towardRoom = wallTexturedFace == WallFaceRule.TowardRoom;
            return (ra == towardRoom) ? WallFace.TowardA : WallFace.TowardB;
        }

        // Creates one wall slab (imported prefab or default cube). flip = textured face toward -Z (cell A).
        GameObject AddWallSlab(Transform parent, float len, float thickness, float offsetZ, bool flip)
        {
            Vector3 size = new Vector3(len, wallHeight, thickness);
            GameObject slab;
            if (wallPrefab != null)
            {
                slab = PrefabFit.Instantiate(wallPrefab, parent, size, wallFitMode);
            }
            else
            {
                slab = new GameObject("Slab");
                slab.transform.SetParent(parent, false);
                var body = new GameObject("Body");
                body.transform.SetParent(slab.transform, false);
                body.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
                body.transform.localScale = size;
                body.AddComponent<MeshFilter>().sharedMesh = GetPrimMesh(PrimitiveType.Cube);
                body.AddComponent<MeshRenderer>().sharedMaterial = GetWallMaterial();
                if (wallColliders) body.AddComponent<BoxCollider>();
            }
            slab.transform.localPosition = new Vector3(0f, 0f, offsetZ);
            slab.transform.localRotation = flip ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            return slab;
        }

        Material GetWallMaterial()
        {
            if (runtimeWallMat != null) return runtimeWallMat;
            if (wallMaterial != null) runtimeWallMat = wallMaterial;
            else
            {
                runtimeWallMat = new Material(FloorStyleFactory.FindLitShader());
                runtimeWallMat.color = new Color(0.93f, 0.91f, 0.85f);   // white HeroQuest walls
                owned.Add(runtimeWallMat);
            }
            return runtimeWallMat;
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
                if (pe.visual != null) pe.animator = pe.visual.GetComponentInChildren<Animator>();
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

        // Multi-cell elements (stairs, blocked squares, furniture): centered on their footprint.
        void BuildSolid(PlacedElement pe)
        {
            ElementDef d = pe.Def; float t = board.tileSize;
            pe.transform.localPosition = new Vector3((d.x + d.w * 0.5f) * t, 0f, (d.y + d.h * 0.5f) * t);
            pe.transform.localRotation = Quaternion.Euler(0f, Yaw(d.facing), 0f);

            // The model is built facing +Z and then rotated: for E/W the model's width and depth are swapped.
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
                nub = true;                                   // small "front face" mark
            }
            Vector3 size = new Vector3(sx, height, sz);
            pe.visual = CreateVisual(d.type, pe.transform, size, color, PrimitiveType.Cube, nub, 0f);
            visuals[d.id] = new VisualInfo { size = size, heightFallback = 0f };
        }

        void BuildDoor(PlacedElement pe)
        {
            ElementDef d = pe.Def;
            pe.transform.localPosition = board.EdgeToLocal(d.edge);
            pe.transform.localRotation = d.edge.horizontal ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
            // Slightly thicker than the wall so it shows on top of it while the wall is still displayed (Zargon view).
            Vector3 size = new Vector3(board.tileSize * 0.98f, wallHeight * 0.95f, wallThickness * 1.5f);
            pe.visual = CreateVisual(d.type, pe.transform, size, new Color(0.45f, 0.28f, 0.12f), PrimitiveType.Cube, false, size.y);
            visuals[d.id] = new VisualInfo { size = size, heightFallback = size.y };
        }

        void BuildMonster(PlacedElement pe)
        {
            ElementDef d = pe.Def; float t = board.tileSize;
            var pawn = pe.gameObject.AddComponent<GridPawn>();
            pawn.board = board;
            pawn.PlaceAt(d.x, d.y);                                       // snapped to the cell center
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
                pe.VisibleToHeroes = pe.Def.kind == ElementKind.Stairs;   // the stairs are always visible
                pe.Trap = TrapState.Hidden;
                ApplyVisibility(pe);
            }
            foreach (int z in Model.InitialZones) RevealZone(z);
        }

        // Shared layer (Board) = seen by both screens. ZargonView = seen by Zargon only.
        // A revealed door visually replaces its wall.
        void ApplyVisibility(PlacedElement pe)
        {
            if (pe.Def.kind == ElementKind.Trap)
            {
                bool triggered = pe.Trap == TrapState.Triggered;
                SetActiveAndLayer(pe.marker, !triggered, zargonLayer);                          // gold marker: Zargon only
                SetActiveAndLayer(pe.highlight, pe.Trap == TrapState.Highlighted, boardLayer);  // suspect cell, no tile
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
            float meshH = prim == PrimitiveType.Cylinder ? 2f : 1f;   // the Unity cylinder is 2 units tall
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

        // ---------- Team tools ----------
        /// <summary>
        /// Logs every element type of the current level that has no prefab bound yet (these still use placeholders).
        /// </summary>
        [ContextMenu("Log Types Without Prefab")]
        void LogTypesWithoutPrefab()
        {
            if (Model == null) { Debug.LogWarning("[Level] Construis d'abord le niveau."); return; }
            var missing = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasTrap = false;
            foreach (var e in Model.Elements)
            {
                if (e.kind == ElementKind.Trap) hasTrap = true;
                if (FindBinding(e.type) == null) missing.Add(e.type);
            }
            if (hasTrap)
            {
                if (FindBinding("TrapMarker") == null) missing.Add("TrapMarker");
                if (FindBinding("TrapHighlight") == null) missing.Add("TrapHighlight");
            }
            string walls = wallPrefab == null ? "  (+ wall: Wall Prefab empty)" : "";
            Debug.Log("[Level] Types sans prefab (" + missing.Count + ") : " + string.Join(", ", new List<string>(missing).ToArray()) + walls);
        }

        /// <summary>Copies to the clipboard a "walls" block in explicit mode holding ALL current walls.</summary>
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

        // Describes an edge from a board cell (S/W side when possible, N/E for the outer border).
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